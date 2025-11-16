using System;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using Asp.Versioning;
using AutoMapper;
using FluentValidation;
using FluentValidation.Resources;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Profiling;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Asp.Versioning.ApiExplorer;
using TestAPIDeployment;

var builder = WebApplication.CreateBuilder(args);
const string myAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services
    .Configure<GzipCompressionProviderOptions>(compressionOptions => compressionOptions.Level = CompressionLevel.Fastest)
    .Configure<MvcNewtonsoftJsonOptions>(jsonOptions =>
    {
        jsonOptions.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        jsonOptions.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
    })
    .Configure<RouteOptions>(routeOptions => routeOptions.LowercaseUrls = true);

var healthChecksBuilder = builder.Services.AddHealthChecks();

#region - registering services

builder.Services.AddHttpClient()
    .AddHttpContextAccessor()
    .AddResponseCompression(compressionOptions =>
    {
        compressionOptions.EnableForHttps = true;
        compressionOptions.Providers.Add<GzipCompressionProvider>();
    })
    .AddApiVersioning(versioningOptions =>
    {
        versioningOptions.DefaultApiVersion = ApiVersion.Default;
        versioningOptions.ReportApiVersions = true;
        versioningOptions.AssumeDefaultVersionWhenUnspecified = true;
    })
    .AddApiExplorer(explorerOptions =>
    {
        explorerOptions.GroupNameFormat = "'v'VVV";
        explorerOptions.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddDataProtection();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressMapClientErrors = true;
        options.SuppressModelStateInvalidFilter = true;
    }).AddNewtonsoftJson((_) => { });

//builder.Services.AddScoped<DatabaseContext>();

#endregion

// MiniProfiler for .NET
// https://miniprofiler.com/dotnet/
builder.Services.AddMiniProfiler(options =>
{
    // Route: /profiler/results-index
    options.RouteBasePath = "/profiler";
    options.ColorScheme = ColorScheme.Dark;
    options.EnableServerTimingHeader = true;
    options.EnableDebugMode = builder.Environment.IsDevelopment();
    options.TrackConnectionOpenClose = true;
}).AddEntityFramework();

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAllOrigin", x =>
    {
        x.AllowAnyHeader();
        x.AllowAnyMethod();
        x.WithOrigins("*",
                "http://localhost:5500",
                "http://localhost:4200",
                "http://localhost:3000",
                "https://localhost:3000",
                "https://staging-lms-portal.crm.ezhubspot.com",
                "https://portal.eztraincomply.com"
                )
            .AllowCredentials();
    });
});

builder.Services.AddSwaggerGen(c =>
{
    c.UseAllOfToExtendReferenceSchemas(); // fixes complex types
});

builder.Host.UseDefaultServiceProvider((context, serviceProviderOptions) =>
{
    serviceProviderOptions.ValidateScopes = context.HostingEnvironment.IsDevelopment();
    serviceProviderOptions.ValidateOnBuild = true;
});

//builder.WebHost.UseKestrel(kestrelOptions => kestrelOptions.AddServerHeader = false);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

ValidatorOptions.Global.DisplayNameResolver = (_, member, _) => member?.Name;
ValidatorOptions.Global.LanguageManager = new LanguageManager { Culture = new CultureInfo("en-US") };

app.UseHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
//app.UseHttpsRedirection();
app.UseHsts();
app.UseRouting();
app.UseCors("AllowAllOrigin");
app.UseResponseCompression();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiniProfiler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // If you want Swagger in production also:
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapControllers();

app.Logger.LogInformation("API");
app.Run();