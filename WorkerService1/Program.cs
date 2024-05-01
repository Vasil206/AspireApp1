using Microsoft.Extensions.Diagnostics.Metrics;
using NATS.Client.Core;
using OpenTelemetry.Metrics;
using WorkerService1;

var builder = Host.CreateApplicationBuilder(args);


builder.AddServiceDefaults();
//builder.AddNatsClient(nats);
//builder.AddNatsJetStream();

builder.Metrics.EnableMetrics(WorkerOptions.Default.MeterName);
builder.Services.AddOpenTelemetry().WithMetrics(conf =>
{
    conf.AddMeter(WorkerOptions.Default.MeterName);
    conf.AddPrometheusHttpListener(opts => opts.UriPrefixes = ["http://Worker"]);
});

builder.Services.Configure<Data>(builder.Configuration.GetSection("Data"));

builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<Load>();

var host = builder.Build();
host.Run();
