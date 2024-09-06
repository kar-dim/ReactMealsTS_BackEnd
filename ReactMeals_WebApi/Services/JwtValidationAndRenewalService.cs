namespace ReactMeals_WebApi.Services;

public class JwtValidationAndRenewalService(IServiceScopeFactory serviceScopeFactory, ILogger<JwtValidationAndRenewalService> logger) : IHostedService, IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private readonly JwtService _jwtService = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<JwtService>();
    private string _managementApiAccessTokenValue = string.Empty; //the token value that is saved to db (so that we won't ask the db all the time for the token)
    public string ManagementApiToken
    {
        get { return _managementApiAccessTokenValue; }
        set { _managementApiAccessTokenValue = value; }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Start the token renewal loop
        Task.Run(async () => await RenewTokenLoop(_cancellationTokenSource.Token), cancellationToken);
        return Task.CompletedTask;
    }

    private async Task RenewTokenLoop(CancellationToken cancellationToken)
    {
        logger.LogInformation("PerformTask called");
        while (!cancellationToken.IsCancellationRequested)
        {
            (bool isExpired, DateTime? dateExpiry, string accessToken) = await _jwtService.IsTokenExpired();

            // Check if the token is expired
            if (isExpired)
            {
                //try to renew the token and get the expiration time
                (DateTime tokenExpiration, bool success, string newAccessToken) = await _jwtService.RenewToken();

                //something bad happened while renewing (network error etc) -> wait some seconds and try again later
                if (!success)
                    await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);
                else
                {
                    //renew token and sleep until it's time to renew the token again
                    TimeSpan sleepTime = tokenExpiration.Subtract(TimeSpan.FromSeconds(30)) - DateTime.Now;
                    _managementApiAccessTokenValue = newAccessToken;
                    await Task.Delay(sleepTime, cancellationToken);
                }
            }
            // The token is still valid, sleep until renew time
            else if (dateExpiry != null)
            {
                _managementApiAccessTokenValue = accessToken;
                TimeSpan sleepTime = (dateExpiry.Value).Subtract(TimeSpan.FromSeconds(30)) - DateTime.Now;
                if (sleepTime.Seconds > 0)
                    await Task.Delay(sleepTime, cancellationToken);
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource.Cancel();
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _cancellationTokenSource.Dispose();
    }
}