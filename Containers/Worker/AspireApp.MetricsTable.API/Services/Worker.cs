using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;
using AspireApp.MetricsTable.Shared;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace AspireApp.MetricsTable.API.Services;

public class Worker : BackgroundService
{
    private readonly string _streamsAndSubjectsPrefix;

    private readonly NatsJSContext _natsJs;
    private readonly ILogger<Worker> _logger;

    private readonly IOptionsMonitor<Data> _dataMonitor;
    private readonly IDisposable? _onDataChange;
    private bool _dataChanged;

    private readonly Dictionary<NameId, CpuRssValue> _measurements;


    public Worker(ILogger<Worker> logger, IOptionsMonitor<Data> dataMonitor, IMeterFactory meterFactory/*, INatsJSContext jsClient*/)
    {
        _streamsAndSubjectsPrefix = WorkerOptions.Default.StreamsAndSubjectsPrefix;
        _natsJs = /*jsClient*/new NatsJSContext(new NatsConnection(new NatsOpts { Url = WorkerOptions.Default.NatsConnection }));

        _dataChanged = false;
        _logger = logger;

        _dataMonitor = dataMonitor;
        _onDataChange = _dataMonitor.OnChange(_ => _dataChanged = true);

        _measurements = [];
        MetricsGetters(meterFactory.Create(WorkerOptions.Default.MeterName));
    }

    private void MetricsGetters(Meter meter)
    {
        LinkedList<Measurement<double>> ObserveCpu() => ObserveValues(val => val.Cpu);
        meter.CreateObservableGauge("worker_processes_usage_cpu", ObserveCpu, unit: "%");

        LinkedList<Measurement<double>> ObserveRss() => ObserveValues(val => val.Rss);
        meter.CreateObservableGauge("worker_processes_usage_rss", ObserveRss, unit: "Mb");
    }

    private LinkedList<Measurement<double>> ObserveValues(Func<CpuRssValue, double> getVal)
    {
        var measurements = new Dictionary<NameId,CpuRssValue>(_measurements);
        LinkedList<Measurement<double>> result = new();
        foreach (var measurement in measurements)
        {
            result.AddLast(new Measurement<double>(getVal(measurement.Value),
                new KeyValuePair<string, object?>("process_name", measurement.Key.Name),
                new KeyValuePair<string, object?>("process_id", measurement.Key.Id)));
        }

        return result;
    }

    private async Task<KeyValuePair<NameId, CpuRssValue>> CalculateCpuRssUsage(Process proc)
    {
        var nameId = new NameId(proc.Id, proc.ProcessName);


        ////////////////////////////////////////////////////////////////////////////////////////////
        double usageCpuTotal;
        try
        {
            if (proc.HasExited) throw new Exception("process has exited");
            TimeSpan startUsageCpu = proc.TotalProcessorTime;
            long startTime = Environment.TickCount64;

            await Task.Delay(_dataMonitor.CurrentValue.Interval / 2);
            proc.Refresh();

            if (proc.HasExited) throw new Exception("process has exited");
            TimeSpan endUsageCpu = proc.TotalProcessorTime;
            long endTime = Environment.TickCount64;

            double usedCpuMs = (endUsageCpu - startUsageCpu).TotalMilliseconds;
            double totalMsPassed = endTime - startTime;
            usageCpuTotal = usedCpuMs / totalMsPassed / Environment.ProcessorCount * 100;
        }
        catch (Win32Exception ex)
        {
            usageCpuTotal = -ex.NativeErrorCode;
            _logger.LogWarning($"{nameId.Name} : {nameId.Id} => {ex.Message}    CPU");
        }
        catch when (proc.HasExited)
        {
            usageCpuTotal = 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            usageCpuTotal = -404;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        double rss;
        try
        {
            if (!proc.HasExited)
                proc.Refresh();
            rss = proc.WorkingSet64 / (1024 * 1024.0);
        }
        catch (Win32Exception ex)
        {
            _logger.LogWarning($"{nameId.Name} : {nameId.Id} => {ex.Message}    RSS");
            rss = -ex.NativeErrorCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            rss = -404;
        }


        return new KeyValuePair<NameId, CpuRssValue>(nameId,
            new CpuRssValue(usageCpuTotal, rss));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {

            ////////////////////////////////////////////////////////////////////////////////////////
            //set
            string[] processNames = _dataMonitor.CurrentValue.ProcessNames;
            var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_dataMonitor.CurrentValue.Interval));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                //going from names to Process
                var processes = new Process[processNames.Length][];
                if (processNames[0] == "--all")
                    processes[0] = Process.GetProcesses();
                else
                {
                    for (int i = 0; i < processNames.Length; i++)
                    {
                        processes[i] = Process.GetProcessesByName(processNames[i]);
                    }
                }


                //making the array of async tasks with calculating of CPU, RSS usage;
                var metricsLoop = new Task<KeyValuePair<NameId, CpuRssValue>>[processes.Length][];
                for (int i = 0; i < metricsLoop.Length; i++)
                {
                    Array.Resize(ref metricsLoop[i], processes[i].Length);
                    for (int j = 0; j < metricsLoop[i].Length; j++)
                    {
                        metricsLoop[i][j] =
                            CalculateCpuRssUsage(processes[i][j]);
                    }
                }


                ClearMeasurementsAndStreams(stoppingToken);


                //wait while works
                foreach (Task[] useCpu in metricsLoop)
                    Task.WaitAll(useCpu, stoppingToken);


                //add to measurements   and   set nats streams
                foreach (var results in metricsLoop)
                {
                    foreach (var result in results)
                    {
                        SetStreamAsync(result.Result, stoppingToken);
                        _measurements[result.Result.Key] = result.Result.Value;
                    }
                }
                

                UploadToNatsParallel(stoppingToken);


                //on data change
                if (_dataChanged)
                {
                    _dataChanged = false;
                    processNames = _dataMonitor.CurrentValue.ProcessNames;
                    timer.Dispose();
                    timer = new(TimeSpan.FromMilliseconds(_dataMonitor.CurrentValue.Interval));
                }

            }

            ////////////////////////////////////////////////////////////////////////////////////////
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
        finally
        {
            _onDataChange?.Dispose();
        }
    }


    private void ClearMeasurementsAndStreams(CancellationToken stoppingToken)
    {
        foreach (var key in _measurements.Keys)
        {
            if (_measurements[key].IsUsed)
            {
                _measurements.Remove(key);

                
                DelStream(key);
            }
            else
            {
                _measurements[key].Used();
            }
        }
        return;

        async void DelStream(NameId key)
        {
            try
            {
                var streamName = new StringBuilder($"{_streamsAndSubjectsPrefix}.{key}");

                streamName.Replace('.', '_');
                streamName.Replace(' ', '_');

                await _natsJs.DeleteStreamAsync(streamName.ToString(), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
            }
        }
    }


    private void UploadToNatsParallel(CancellationToken stoppingToken)
    {
        var measurements = new Dictionary<NameId, CpuRssValue>(_measurements);
        Parallel.ForEach(measurements, UploadToNatsAsync);
        return;

        async void UploadToNatsAsync(KeyValuePair<NameId, CpuRssValue> measurement)
        {
            try
            {
                var ack = await _natsJs.PublishAsync(
                    $"{_streamsAndSubjectsPrefix}.{measurement.Key}",
                    $"cpu: {measurement.Value.Cpu} || rss: {measurement.Value.Rss}",
                    cancellationToken: stoppingToken);

                ack.EnsureSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
            }
        }
    }

    private async void SetStreamAsync(KeyValuePair<NameId, CpuRssValue> measurement,
        CancellationToken stoppingToken)
    {
        StringBuilder streamNameOrSubject = new();
        streamNameOrSubject.Append($"{_streamsAndSubjectsPrefix}.{measurement.Key.AsString()}");
        string subject = streamNameOrSubject.ToString();

        streamNameOrSubject.Replace('.', '_');
        streamNameOrSubject.Replace(' ', '_');
        string streamName = streamNameOrSubject.ToString();

        try
        {
            INatsJSStream? stream;

            if (_measurements.ContainsKey(measurement.Key))
            {
                stream = await _natsJs.GetStreamAsync(streamName, cancellationToken: stoppingToken);
            }
            else
            {
                stream = await _natsJs.CreateStreamAsync(
                    new StreamConfig(streamName, [ subject ]),
                    stoppingToken);
            }

            if (stream.Info.Config.MaxMsgs != 1)
            {
                stream.Info.Config.MaxMsgs = 1;
                await stream.UpdateAsync(stream.Info.Config, stoppingToken);
            }

        }
        catch (NatsJSApiException e) when (e.Error.ErrCode == 10058 || e.Error.ErrCode == 10059)
        {
            _logger.LogWarning(e.Message);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }
    }

}
