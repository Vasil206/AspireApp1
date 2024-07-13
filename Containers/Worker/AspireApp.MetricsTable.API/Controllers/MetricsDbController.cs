using System.Diagnostics.Metrics;
using AspireApp.MetricsTable.Shared;
using Microsoft.AspNetCore.Mvc;

namespace AspireApp.MetricsTable.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MetricsDbController : ControllerBase
    {
        private readonly MeterListener _listener;

        private readonly Meters _meters;


        private void OnMeasurementRecorded<T>(
            Instrument instrument,
            T value,
            ReadOnlySpan<KeyValuePair<string, object?>> tags,
            object? state)
        {
            var tagsArray = tags.ToArray();

            var measurementIndex = _meters[instrument.Meter.Name][instrument.Name]
                .FindIndex(m => m.Tags.SequenceEqual(tagsArray));

            if (measurementIndex != -1)
            {
                _meters[instrument.Meter.Name][instrument.Name][measurementIndex].Value = Convert.ToDouble(value);
            }
            else
            {
                _meters[instrument.Meter.Name][instrument.Name].Add(new(tagsArray, Convert.ToDouble(value)));
            }
        }

        public MetricsDbController()
        {
            _meters = new Meters();
            _listener = new MeterListener();
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                _meters.TryAdd(instrument.Meter.Name, []);
                _meters[instrument.Meter.Name].TryAdd(instrument.Name, []);
                listener.EnableMeasurementEvents(instrument);
            };
            _listener.SetMeasurementEventCallback<double>(OnMeasurementRecorded);
            _listener.SetMeasurementEventCallback<int>(OnMeasurementRecorded);
            _listener.Start();
        }

        [HttpGet]
        public ActionResult<Meters> GetAll()
        {
            _listener.RecordObservableInstruments();
            return Ok(_meters);
        }

        [HttpGet("meters")]
        public ActionResult<IEnumerable<string>> GetMeterNames()
        {
            _listener.RecordObservableInstruments();
            return Ok(_meters.Keys);
        }

        [HttpGet("meters/{meterName}")]
        public ActionResult<IEnumerable<string>> GetInstrumentNames(string meterName)
        {
            _listener.RecordObservableInstruments();
            if (_meters.TryGetValue(meterName, out var meterInstruments))
            {
                return Ok(meterInstruments.Keys);
            }

            return NotFound("This Meter does not exists");
        }

        [HttpGet("meters/{meterName}/{instrumentName}")]
        public ActionResult<IEnumerable<UserMeasurement>> GetMeasurements(string meterName, string instrumentName)
        {
            _listener.RecordObservableInstruments();
            if (!_meters.TryGetValue(meterName, out var meter))
            {
                return NotFound("This Meter does not exists");
            }

            if (!meter.TryGetValue(instrumentName, out var measurements))
            {
                return NotFound("This instrument does not exists in this Meter");
            }

            return Ok(measurements);
        }
    }
}
