using functionserver.Services;
using Core.Helpers;
using Core.Interfaces;
using functionserver;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration.Get<Configuration>();
config.FunctionDirectory = AppContext.BaseDirectory;

builder.Services.AddSingleton(config);
builder.Services.AddSingleton<FunctionExecutor>();
builder.Services.AddSingleton(new Encryption(config.EncryptionKey));
var runningFunctionCache = new RunningFunctionCache();
builder.Services.AddSingleton(
    runningFunctionCache);
builder.Services.AddSingleton(new FunctionManager(runningFunctionCache));

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = int.MaxValue; // if don't set default value is: 30 MB
    
    if(config.http_port.HasValue)
        options.ListenAnyIP(config.http_port.Value, listenOptions =>
        {
        });
    if(config.https_port.HasValue)
        options.ListenAnyIP(config.https_port.Value, listenOptions =>
        {
            listenOptions.UseHttps();
        });
});

Environment.CurrentDirectory = AppContext.BaseDirectory;
var logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
ILogger iLogger = new LoggerFactory().AddSerilog(logger).CreateLogger("sharpfaas");

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);
builder.Services.AddSingleton(iLogger);


if (string.IsNullOrWhiteSpace(config.FunctionDirectory))
{
    var s3 = new S3Store(config.BucketName, config.BucketAccessSecret, config.BucketAccessKey, config.BucketRegion,
        iLogger);

    builder.Services.AddSingleton<IFunctionStore>(s3);
}
else
{
    Directory.CreateDirectory(config.FunctionDirectory);
    
    var ftp = new FtpStore(config, config.FunctionDirectory,iLogger);
    
    builder.Services.AddSingleton<IFunctionStore>(ftp);
}

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseRouting();

app.UseSwagger();
app.UseSwaggerUI();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();