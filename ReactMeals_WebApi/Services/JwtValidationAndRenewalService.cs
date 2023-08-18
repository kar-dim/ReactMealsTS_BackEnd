using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReactMeals_WebApi.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

public class JwtValidationAndRenewalService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private CancellationTokenSource _cancellationTokenSource;
    private readonly ILogger<JwtValidationAndRenewalService> _logger;

    public JwtValidationAndRenewalService(IServiceScopeFactory scopeFactory, ILogger<JwtValidationAndRenewalService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Start the token renewal loop
        _cancellationTokenSource = new CancellationTokenSource();
        Task.Run(async () => await RenewTokenLoop(_cancellationTokenSource.Token));
        return Task.CompletedTask;
    }

    private async Task RenewTokenLoop(CancellationToken cancellationToken)
    {
        _logger.LogInformation("JwtValidationService: PerformTask called");
        using (var scope = _scopeFactory.CreateScope())
        {
            var jwtService = scope.ServiceProvider.GetRequiredService<JwtService>();

            while (!cancellationToken.IsCancellationRequested)
            {
                (bool isExpired, DateTime? dateExpiry) = await jwtService.IsTokenExpired();

                // Check if the token is expired
                if (isExpired)
                {
                    // Renew the token and get the expiration time
                    (DateTime tokenExpiration, bool success) = await jwtService.RenewToken();

                    //something bad happened while renewing (network error etc) -> wait some seconds and try again later
                    if (!success)
                    {
                        await Task.Delay(20 * 1000, cancellationToken);
                    }
                    else
                    {
                        // Calculate the time to sleep (minus 30 seconds)
                        TimeSpan sleepTime = tokenExpiration.Subtract(TimeSpan.FromSeconds(30)) - DateTime.Now;

                        // Sleep until it's time to renew the token (plus some seconds)
                        await Task.Delay(sleepTime, cancellationToken);
                    }
                }
                else
                {
                    if (dateExpiry != null)
                    {
                        // The token is still valid, sleep
                        TimeSpan sleepTime = ((DateTime)dateExpiry).Subtract(TimeSpan.FromSeconds(30)) - DateTime.Now;
                        if (sleepTime.Seconds > 0)
                            await Task.Delay(sleepTime, cancellationToken);
                    }
                    
                }
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource?.Cancel();
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
    }
}
