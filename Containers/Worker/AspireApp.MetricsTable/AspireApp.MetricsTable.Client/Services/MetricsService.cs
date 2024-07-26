using System.Net.Http.Json;
using AspireApp.MetricsTable.Shared;

namespace AspireApp.MetricsTable.Client.Services;

public class MetricsService : IMetricsService
{
    private readonly HttpClient _http;

    public MetricsService(HttpClient http)
    {
        _http = http;
    }


    public async Task<Meters> GetMeters()
    {
        var meters = await _http.GetFromJsonAsync<Meters>("api/MetricsDb/meters");

        return meters!;
    }


    public async Task<List<string>> GetMeterNames()
    {
        var meterNames = await _http.GetFromJsonAsync<List<string>>("api/MetricsDb/meter_names");

        return meterNames!;
    }

    public async Task<List<string>> GetInstrumentNames(string meterName)
    {
        var instrumentNames = await _http.GetFromJsonAsync<List<string>>($"api/MetricsDb/{meterName}/instrument_names");

        return instrumentNames!;
    }

    public async Task<List<UserMeasurement>> GetMeasurements(string meterName, string instrumentName)
    {
        var measurements =
            await _http.GetFromJsonAsync<List<UserMeasurement>>(
                $"api/MetricsDb/{meterName}/{instrumentName}/measurements");

        return measurements!;
    }
}