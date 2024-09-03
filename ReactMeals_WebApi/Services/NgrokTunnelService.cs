using CliWrap;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Server;
using CliWrap.Exceptions;

namespace ReactMeals_WebApi.Services
{
    public class NgrokTunnelService : BackgroundService
    {
        private readonly IServer _server;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ILogger<NgrokTunnelService> _logger;
        private readonly IConfiguration _config;

        public NgrokTunnelService(IConfiguration config, IServer server, IHostApplicationLifetime hostApplicationLifetime, ILogger<NgrokTunnelService> logger)
        {
            _config = config;
            _server = server;
            _hostApplicationLifetime = hostApplicationLifetime;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await WaitForApplicationStarted();

            var urls = _server.Features.Get<IServerAddressesFeature>()!.Addresses;
            var localUrl = urls.Single(u => u.StartsWith("http://"));
            var ngrokDomain = _config["ngrok:url"];
            _logger.LogInformation("Starting ngrok tunnel for {LocalUrl}", localUrl);
            var ngrokTask = StartNgrokTunnel(localUrl, ngrokDomain, stoppingToken);

            _logger.LogInformation("Public ngrok URL: {NgrokPublicUrl}", ngrokDomain);

            await ngrokTask;

            _logger.LogInformation("Ngrok tunnel stopped");
        }

        private Task WaitForApplicationStarted()
        {
            var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _hostApplicationLifetime.ApplicationStarted.Register(() => completionSource.TrySetResult());
            return completionSource.Task;
        }

        private async Task<CommandTask<CommandResult>> StartNgrokTunnel(string localUrl, string ngrokUrl, CancellationToken stoppingToken)
        {
            try
            {
                //kill existing ngrok
                //taskkill / f / im ngrok.exe
                await Cli.Wrap("taskkill")
                        .WithArguments(args => args
                        .Add("/f")
                        .Add("/im")
                        .Add("ngrok.exe"))
                    .WithStandardOutputPipe(PipeTarget.ToDelegate(s => _logger.LogDebug(s)))
                    .WithStandardErrorPipe(PipeTarget.ToDelegate(s => _logger.LogError(s)))
                    .ExecuteAsync(stoppingToken);
            } catch (CommandExecutionException){ /*ignore, don't care if no ngrok processes are killed*/ }

            var ngrokTask = Cli.Wrap("ngrok")
                .WithArguments(args => args
                    .Add("http")
                    .Add("--domain="+ngrokUrl)
                    .Add(localUrl)
                    .Add("--log")
                    .Add("stdout"))
                .WithStandardOutputPipe(PipeTarget.ToDelegate(s => _logger.LogDebug(s)))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(s => _logger.LogError(s)))
                .ExecuteAsync(stoppingToken);
            return ngrokTask;
        }
    }
}