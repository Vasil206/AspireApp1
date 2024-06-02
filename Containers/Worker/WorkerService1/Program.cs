using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Diagnostics.Metrics;
using OpenTelemetry.Metrics;
using WorkerService1;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
//builder.AddNatsClient(builder.Configuration.GetConnectionString("nats") ?? throw new ArgumentNullException());    only with containers from AppHost
//builder.AddNatsJetStream();

builder.Metrics.EnableMetrics(WorkerOptions.Default.MeterName);

builder.Services.AddOpenTelemetry().WithMetrics(conf =>
{
    conf.AddPrometheusHttpListener(opts => opts.UriPrefixes = [WorkerOptions.Default.PrometheusConnection]);
    //conf.AddPrometheusExporter(); // doesn't work in docker          (1)
    conf.AddMeter(WorkerOptions.Default.MeterName);
});

builder.Services.AddOptions<Data>().BindConfiguration("Data").ValidateDataAnnotations().ValidateOnStart();

builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<Load>();

var host = builder.Build();

//host.MapPrometheusScrapingEndpoint("/metrics");                 //(1')

host.Run();
