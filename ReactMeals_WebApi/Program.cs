using App.Metrics.Formatters.Prometheus;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ReactMeals_WebApi.Models;
using ReactMeals_WebApi.Services;
using System.Reflection.PortableExecutable;

var builder = WebApplication.CreateBuilder(args);

//metrics
builder.Host.UseMetricsWebTracking();
builder.Host.UseMetricsEndpoints(endpointsOptions =>
{
    endpointsOptions.MetricsTextEndpointOutputFormatter = new MetricsPrometheusTextOutputFormatter();
    endpointsOptions.MetricsEndpointOutputFormatter = new MetricsPrometheusProtobufOutputFormatter();
    endpointsOptions.EnvironmentInfoEndpointEnabled = false;
});

builder.Host.ConfigureAppMetricsHostingConfiguration(options =>
{
    options.MetricsEndpoint = "/meals_metrics";
    options.MetricsTextEndpoint = "/meals_metrics-text";
});

// Add services to the container.
builder.Services.Configure<JimmysFoodzillaDatabaseSettings>(builder.Configuration.GetSection("JimmysFoodzillaDatabase"));
builder.Services.AddSingleton<JimmysFoodzillaService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

//metrics
//builder.Services.Configure<KestrelServerOptions>(options => { { options.AllowSynchronousIO = true;  } })
builder.Services.AddMetrics();

//cors
var allowFrontendOnly = "allowFrontendOnly";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: allowFrontendOnly, 
        policy => {  
            policy.WithOrigins("http://localhost:3001", "http://localhost:3000");
            policy.AllowAnyMethod();
            policy.WithHeaders("X-Requested-With", "Content-Type");
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(allowFrontendOnly);


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
