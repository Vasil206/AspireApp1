﻿namespace AspireApp.MetricsTable.API.Services
{
    internal class Load : BackgroundService
    {
        private static void MemoryCpuLoad(long timeExec)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            DateTime start = DateTime.Now;
            long[] arr = GC.AllocateArray<long>(100000000, true);
            int i = 1;
            while (i <= 100000000 && (DateTime.Now - start).TotalSeconds <= timeExec)
            {
                arr[i] = arr[i - 1] + 1;
                i++;
            }
            while ((DateTime.Now - start).TotalSeconds <= timeExec)
            {
            }

            Task.Delay(Math.Max(Convert.ToInt32(timeExec - (DateTime.Now - start).TotalSeconds) * 1000, 0));
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            PeriodicTimer timer = new(TimeSpan.FromMinutes(3));
            Random rand = new(DateTime.Now.Millisecond);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    long timeExecSeconds = rand.Next(1, 170);
                    MemoryCpuLoad(timeExecSeconds);
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}