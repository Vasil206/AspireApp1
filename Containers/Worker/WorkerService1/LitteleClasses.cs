namespace WorkerService1;

internal readonly struct NameId(int id, string name)
{
    public readonly int Id = id;
    public readonly string Name = name;

    public override string ToString() => $"{Name}#{Id}";
}

internal class CpuRssValue(double cpu, double rss)
{
    public readonly double Cpu = cpu;
    public readonly double Rss = rss;
    public bool IsUsed = false;
}
public class Data
{
    public int Interval { get; init; }
    public string[] ProcessNames { get; init; } = default!;
}

public class WorkerOptions
{
    public static readonly WorkerOptions Default = new();
    public string PrometheusConnection { get; set; } = "http://localhost:1234/";
    public string NatsConnection { get; set; } = "nats://0.0.0.127:4222/";
    public string StreamsAndSubjectsPrefix { get; set; } = "workerMetrics";
    public string MeterName { get; set; } = "cpu_rss_watcher";
}