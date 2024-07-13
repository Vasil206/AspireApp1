using System.Diagnostics.Metrics;

namespace AspireApp.MetricsTable.Shared;

public class Meters : Dictionary<string, Instruments>;

public class Instruments : Dictionary<string, List<UserMeasurement>>;

public class UserMeasurement
{
    public UserMeasurement(KeyValuePair<string, object?>[] tags, double val = 0)
    {
        Tags = tags;
        Value = val;
    }
    public double Value { get; set; } = 0;

    public KeyValuePair<string, object?>[] Tags { get; set; }
}