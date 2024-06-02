using System.ComponentModel.DataAnnotations;

namespace WorkerService1;

internal readonly struct NameId(int id, string name)
{
    public readonly int Id = id;
    public readonly string Name = name;

    public string AsString() => $"{Name}#{Id}";
}

internal struct CpuRssValue(double cpu, double rss)
{
    public readonly double Cpu = cpu;
    public readonly double Rss = rss;
    public bool IsUsed = false;

    public void Used()
    {
        IsUsed = true;
    }
}
public class Data
{
    [Range(100, int.MaxValue)]
    public int Interval { get; set; }

    [Required]
    public string[] ProcessNames { get; set; } = default!;
}

public class WorkerOptions
{
    public static readonly WorkerOptions Default = new();
    public string PrometheusConnection { get; set; } = "http://cpu_rss_wacher:1234/";
    public string NatsConnection { get; set; } = "nats://nats_server:4222/";
    public string StreamsAndSubjectsPrefix { get; set; } = "workerMetrics";
    public string MeterName { get; set; } = "cpu_rss_watcher";
}