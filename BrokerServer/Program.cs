using BrokerServer.Hubs;
using BrokerServer.Models;
using BrokerServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024;
    options.EnableDetailedErrors = true;
});
builder.Services.AddSingleton<CodeGenerator>();
builder.Services.AddSingleton<SessionManager>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true);
    });
});

var port = Environment.GetEnvironmentVariable("PORT") ?? "80";
builder.WebHost.UseUrls($"http://+:{port}");

var app = builder.Build();

app.UseCors();
app.MapHub<RemoteSessionHub>("/hub");

app.MapGet("/", () => "Broker Server OK");

app.Run();
