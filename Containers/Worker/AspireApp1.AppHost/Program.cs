
var builder = DistributedApplication.CreateBuilder(args);

var webApi = builder.AddProject<Projects.AspireApp_MetricsTable_API>("aspireapp-metricstable-api")
    /*.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://aspire_dashboard:18889")*/;

builder.AddProject<Projects.AspireApp_MetricsTable>("aspireapp-blazormetricstable")
    .WithReference(webApi);




builder.Build().Run();
