using AspireApp.MetricsTable.API.Services;
using AspireApp.MetricsTable.Shared;
using Microsoft.Extensions.Diagnostics.Metrics;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Setup metrics
builder.Metrics.EnableMetrics(WorkerOptions.Default.MeterName);

builder.Services.AddOpenTelemetry().WithMetrics(conf =>
{
    //conf.AddPrometheusHttpListener(opts => opts.UriPrefixes = [WorkerOptions.Default.PrometheusConnection]);
    conf.AddPrometheusExporter(); // doesn't work in docker          (1)
    conf.AddMeter(WorkerOptions.Default.MeterName);
});

// Add options
builder.Services.AddOptions<Data>().BindConfiguration("Data").ValidateDataAnnotations().ValidateOnStart();

// Add services to the container.
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<Load>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapPrometheusScrapingEndpoint("/metrics");                 //(1')

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
