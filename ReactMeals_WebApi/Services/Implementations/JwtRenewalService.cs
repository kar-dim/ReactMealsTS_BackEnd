using ReactMeals_WebApi.Services.Interfaces;

namespace ReactMeals_WebApi.Services.Implementations;

public class JwtRenewalService(IServiceScopeFactory serviceScopeFactory, ILogger<JwtRenewalService> logger) : IJwtRenewalService
{
    //written by the background renewal loop, read by request threads -> volatile for visibility
    private volatile string _managementApiToken = string.Empty;
    public string ManagementApiToken
    {
        get => _managementApiToken;
        set => _managementApiToken = value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        //start the token renewal loop
        Task.Run(async () => await RenewTokenLoop(cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }

    //Main service loop which renews the token
    public async Task RenewTokenLoop(CancellationToken cancellationToken)
    {
        logger.LogInformation("Renew token main loop started");
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // JwtService is created fresh each loop; the access token stays in memory.
                using var scope = serviceScopeFactory.CreateScope();
                var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

                var newAccessToken = await jwtService.RenewToken();
                if (newAccessToken == null)
                {
                    ManagementApiToken = string.Empty;
                    logger.LogError("Error while renewing token, waiting 20 seconds and trying again...");
                    await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);
                    continue;
                }
                ManagementApiToken = newAccessToken.TokenValue;
                logger.LogInformation("Successfully renewed token");
                TimeSpan sleepTime = newAccessToken.ExpiryDate.Subtract(TimeSpan.FromSeconds(30)) - DateTime.Now;
                if (sleepTime > TimeSpan.Zero)
                    await Task.Delay(sleepTime, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                //normal shutdown
                break;
            }
            catch (Exception ex)
            {
                // if something unexpected happens, log and retry after a short delay
                logger.LogError("Unexpected error in token renewal loop: {Error}", ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
