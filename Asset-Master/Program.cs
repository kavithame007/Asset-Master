using Hangfire;
using AutoMapper;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions;
using Microsoft.OpenApi.Models;
using Asset_Master;
using System.Configuration;
using System.Net;
using System.Text.Json.Serialization;
using Asset_Master.Interfaces;
using Asset_Master.Repository;
using Asset_Master.Helpers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

//var builder = new ConfigurationBuilder()
//        .SetBasePath(env.ContentRootPath)
//        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
//        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);
//Configuration = builder.Build();

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
// Access configuration values
//var connectionString = builder.Configuration.GetConnectionString("DataBase");
//var sharePointSiteUrl = builder.Configuration["SharePointSiteUrl"];
//var sharePointUsername = builder.Configuration["SharePointUsername"];
//var sharePointPassword = builder.Configuration["SharePointPassword"];

// ... other configurations and services ...

//var app = builder.Build();
// ... app.Run() or other app configurations ...
builder.Services.AddSwaggerGen(c =>
{ //<-- NOTE 'Add' instead of 'Configure'
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Asset Tool",
        Version = "v1"
    });
});

builder.Services.AddHangfire(config => config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
.UseSimpleAssemblyNameTypeSerializer()
.UseDefaultTypeSerializer()
.UseMemoryStorage()
);
builder.Services.AddDbContext<APIDbContext>(options => options.UseMySql(builder.Configuration.GetConnectionString("DataBase"), new MySqlServerVersion(new Version())));
builder.Services.AddControllers().AddJsonOptions(x =>
{
    // serialize enums as strings in api responses (e.g. Role)
    x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

    // ignore omitted parameters on models to enable optional params (e.g. User update)
    x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddScoped<IAsset, AssetRepository>();
builder.Services.AddScoped<Isharepoint_Asset, sharepoint_AssetRepository>();

builder.Services.AddHangfireServer();
var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();


app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
});
app.UseMiddleware<ErrorHandlerMiddleware>();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html"); ;
app.UseHangfireDashboard();
app.Run();
BackgroundJob.Enqueue(() => Console.WriteLine("Fire-and-forget Job Executed"));


/*
using Hangfire;
using Hangfire.MemoryStorage;
using SmartCV.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHangfire(config => config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
.UseSimpleAssemblyNameTypeSerializer()
.UseDefaultTypeSerializer()
.UseMemoryStorage()
);

builder.Services.AddScoped<IAsset, AssetRepository>();
builder.Services.AddHangfireServer();
var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartCV.Services");
    });
}
app.UseHangfireDashboard();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
BackgroundJob.Enqueue(() => Console.WriteLine("Fire-and-forget Job Executed"));*/