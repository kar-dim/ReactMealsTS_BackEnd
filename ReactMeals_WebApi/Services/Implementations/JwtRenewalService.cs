using ReactMeals_WebApi.Services.Interfaces;

namespace ReactMeals_WebApi.Services.Implementations;

public class JwtRenewalService(IServiceScopeFactory serviceScopeFactory, ILogger<JwtRenewalService> logger) : IJwtRenewalService
{
    private readonly IJwtService jwtService = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<IJwtService>();
    //cached token value loaded from the database to avoid repeated DB queries
    public string ManagementApiToken { get; set; } = string.Empty;

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
            logger.LogInformation("Retrieving local token...");
            var token = await jwtService.RetrieveToken();
            // If no token is found, or it is expired, we must renew it
            if (token == null || token.ExpiryDate <= DateTime.Now)
            {
                logger.LogInformation("No token found in db, or it is expired, renewing...");
                //try to renew the token and get the expiration time
                var newAccessToken = await jwtService.RenewToken();
                //something bad happened while renewing (network error etc) -> wait some seconds and try again later
                if (newAccessToken == null)
                {
                    logger.LogError("Error while renewing token, waiting 20 seconds and trying again...");
                    await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);
                    continue;
                }
                //renew token and sleep until it's time to renew the token again
                TimeSpan sleepTime = newAccessToken.ExpiryDate.Subtract(TimeSpan.FromSeconds(30)) - DateTime.Now;
                ManagementApiToken = newAccessToken.TokenValue;
                logger.LogInformation("Successfully renewed token");
                await Task.Delay(sleepTime, cancellationToken);
                
            }
            // The token is still valid, sleep until renew time
            else
            {
                logger.LogInformation("Successfully retrieved local token. It will expire at: " + token.ExpiryDate.ToString("dd/MM/yyyy HH:mm"));
                ManagementApiToken = token.TokenValue;
                TimeSpan sleepTime = token.ExpiryDate.Subtract(TimeSpan.FromSeconds(30)) - DateTime.Now;
                if (sleepTime.Seconds > 0)
                    await Task.Delay(sleepTime, cancellationToken);
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