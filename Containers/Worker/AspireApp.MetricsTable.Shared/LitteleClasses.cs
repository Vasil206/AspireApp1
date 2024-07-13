using System.ComponentModel.DataAnnotations;

namespace AspireApp.MetricsTable.Shared;

public readonly struct NameId(int id, string name) : IEquatable<NameId>
{
    public readonly int Id = id;
    public readonly string Name = name;

    public string AsString() => $"{Name}#{Id}";

    public bool Equals(NameId other)
    {
        return Id == other.Id && Name == other.Name;
    }

    public override bool Equals(object? obj)
    {
        return obj is NameId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name);
    }
}

public struct CpuRssValue(double cpu, double rss)
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
    public int Interval { get; init; } = 1000;

    [Required]
    public string[] ProcessNames { get; set; } = default!;
}

public class WorkerOptions
{
    public static readonly WorkerOptions Default = new();
    public string PrometheusConnection { get; set; } = "http://cpu_rss_wacher:1234/";
    public string NatsConnection { get; } = "nats://nats_server:4222/";
    public string StreamsAndSubjectsPrefix { get; } = "workerMetrics";
    public string MeterName { get; } = "cpu_rss_watcher";
}