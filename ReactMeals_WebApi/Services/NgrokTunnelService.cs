﻿using CliWrap;
using CliWrap.Exceptions;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace ReactMeals_WebApi.Services;

public class NgrokTunnelService(IConfiguration config, IServer server, IHostApplicationLifetime hostApplicationLifetime, ILogger<NgrokTunnelService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await WaitForApplicationStarted();
        string localUrl = server.Features.Get<IServerAddressesFeature>().Addresses.Single(u => u.StartsWith("http://"));
        await StartNgrokTunnelAsync(localUrl, config["ngrok:url"], stoppingToken);
    }

    private Task WaitForApplicationStarted()
    {
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        hostApplicationLifetime.ApplicationStarted.Register(() => completionSource.TrySetResult());
        return completionSource.Task;
    }

    private async Task StartNgrokTunnelAsync(string localUrl, string ngrokUrl, CancellationToken stoppingToken)
    {
        try
        {
            //kill existing ngrok: taskkill / f / im ngrok.exe
            await Cli.Wrap("taskkill")
                .WithArguments(args => args.Add("/f").Add("/im").Add("ngrok.exe"))
                .WithStandardOutputPipe(PipeTarget.ToDelegate(s => logger.LogInformation(s)))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(s => logger.LogError(s)))
                .ExecuteAsync(stoppingToken);
            logger.LogInformation("Killed ngrok service...");
            await Task.Delay(3000, stoppingToken);
        }
        catch (CommandExecutionException)
        {
            logger.LogInformation("No existing Ngrok tunnel is running...");
        }

        logger.LogInformation("Starting ngrok tunnel for {LocalUrl}", localUrl);
        try
        {
            //call ngrok
            await Cli.Wrap("ngrok")
                .WithArguments(args => args.Add("http").Add("--domain=" + ngrokUrl).Add(localUrl).Add("--log").Add("stdout"))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(s => logger.LogError(s)))
                .ExecuteAsync(stoppingToken);
        }
        catch (CommandExecutionException)
        {
            logger.LogError("Could not start ngrok!");
        }
    }
}