using AspireApp.MetricsTable.Shared;

namespace AspireApp.MetricsTable.Client.Services;

public interface IMetricsService
{
    Task<Meters> GetMeters();

    Task<List<string>> GetMeterNames();
    Task<List<string>> GetInstrumentNames(string meterName);

    Task<List<UserMeasurement>> GetMeasurements(string meterName, string instrumentName);
}