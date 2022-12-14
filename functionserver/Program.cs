using functionserver.Services;
using Core.Helpers;
using Core.Interfaces;
using functionserver;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration.Get<Configuration>();

config.FunctionDirectory = Path.Combine(AppContext.BaseDirectory,"Functions");
Directory.CreateDirectory(config.FunctionDirectory);
builder.Services.AddSingleton(config);

Environment.CurrentDirectory = AppContext.BaseDirectory;
var logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
ILogger iLogger = new LoggerFactory().AddSerilog(logger).CreateLogger("sharpfaas");

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);
builder.Services.AddSingleton(iLogger);

if (!string.IsNullOrWhiteSpace(config.BucketName))
{
    var s3 = new S3Store(config.BucketName, config.BucketAccessSecret, config.BucketAccessKey, config.BucketRegion,
        iLogger);

    builder.Services.AddSingleton<IFunctionStore>(s3);
}
else
{
    var ftp = new FtpStore(config, config.FunctionDirectory,iLogger);
    
    builder.Services.AddSingleton<IFunctionStore>(ftp);
}

builder.Services.AddSingleton(new Encryption(config.EncryptionKey));
builder.Services.AddSingleton<FunctionExecutor>();
builder.Services.AddSingleton<RunningFunctionCache>();
builder.Services.AddSingleton<FunctionManager>();

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