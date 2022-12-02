using functionserver.Services;
using Core.Helpers;
using Core.Interfaces;
using functionserver;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

var config = builder.Configuration.Get<Configuration>();

builder.Services.AddSingleton(config);
builder.Services.AddSingleton<FunctionExecutor>();
builder.Services.AddSingleton(new Encryption(config.EncryptionKey));
builder.Services.AddSingleton(
    new RunningFunctionCache());

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = int.MaxValue; // if don't set default value is: 30 MB
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