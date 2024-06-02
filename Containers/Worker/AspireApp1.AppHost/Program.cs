
var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.WorkerService1>("workerservice1")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://aspire_dashboard:18889"); //domain where aspire dashboard container gets  open telemetry


builder.Build().Run();
