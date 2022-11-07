using functionserver;
using functionserver.Hubs;
using functionserver.Services;
using Core.Helpers;
using Core.Interfaces;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

var config = new Configuration()
{
    FunctionDirectory = builder.Configuration["FunctionDirectory"],
    Ftp = new FunctionFtp
    {
        FtpServer = builder.Configuration["FtpHost"],
        FtpPort = builder.Configuration["FtpPort"],
        FtpUser = builder.Configuration["FtpUser"],
        FtpPassword = builder.Configuration["FtpPassword"],
        FtpFolder = builder.Configuration["FtpFolder"]
    }
};

builder.Services.AddSingleton(config);
builder.Services.AddSingleton<FunctionExecutor>();
builder.Services.AddSingleton(new Encryption(builder.Configuration["EncryptionKey"]));

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = int.MaxValue; // if don't set default value is: 30 MB
});


using ILoggerFactory loggerFactory =
    LoggerFactory.Create(builder =>
        builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "hh:mm:ss ";
            })
            .AddFile($"functionserver.txt", fileLoggerOpts => { fileLoggerOpts.MaxRollingFiles = 10;}));

var logger = loggerFactory.CreateLogger("Console");

var ftp = new FtpStore(config.Ftp, config.FunctionDirectory,logger);

builder.Services.AddSingleton(logger);

builder.Services.AddSingleton<IFunctionStore>(ftp);
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseRouting();
if(!string.IsNullOrWhiteSpace(builder.Configuration["Port"]))
    app.Urls.Add($"http://+:{builder.Configuration["Port"]}");

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<FunctionsHub>("/functions");
    endpoints.MapControllers();
});

app.Run();