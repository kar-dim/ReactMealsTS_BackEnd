using Azure.Core;
using ReactMeals_WebApi.Services;

public class JwtValidationAndRenewalService : IHostedService, IDisposable
{
    private string _className;
    private readonly IServiceScopeFactory _scopeFactory;
    private CancellationTokenSource _cancellationTokenSource;
    private readonly ILogger<JwtValidationAndRenewalService> _logger;
    private string _managementApiAccessTokenValue; //the token value that is saved to db (so that we won't ask the db all the time for the token)
    public string ManagementApiAccessTokenValue
    {
        get { return _managementApiAccessTokenValue; }
        set { _managementApiAccessTokenValue = value; }
    }
    public JwtValidationAndRenewalService(IServiceScopeFactory scopeFactory, ILogger<JwtValidationAndRenewalService> logger)
    {
        _className = nameof(JwtValidationAndRenewalService) + ": ";
        _cancellationTokenSource = new CancellationTokenSource();
        _scopeFactory = scopeFactory;
        _logger = logger;
        _managementApiAccessTokenValue = string.Empty;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Start the token renewal loop
        Task.Run(async () => await RenewTokenLoop(_cancellationTokenSource.Token));
        return Task.CompletedTask;
    }

    private async Task RenewTokenLoop(CancellationToken cancellationToken)
    {
        _logger.LogInformation(_className + "PerformTask called");
        using (var scope = _scopeFactory.CreateScope())
        {
            var jwtService = scope.ServiceProvider.GetRequiredService<JwtService>();

            while (!cancellationToken.IsCancellationRequested)
            {
                (bool isExpired, DateTime? dateExpiry, string accessToken) = await jwtService.IsTokenExpired();

                // Check if the token is expired
                if (isExpired)
                {
                    // Renew the token and get the expiration time
                    (DateTime tokenExpiration, bool success, string newAccessToken) = await jwtService.RenewToken();

                    //something bad happened while renewing (network error etc) -> wait some seconds and try again later
                    if (!success)
                    {
                        await Task.Delay(20 * 1000, cancellationToken);
                    }
                    else
                    {
                        // Calculate the time to sleep (minus 30 seconds)
                        TimeSpan sleepTime = tokenExpiration.Subtract(TimeSpan.FromSeconds(30)) - DateTime.Now;
                        _managementApiAccessTokenValue = newAccessToken;
                        // Sleep until it's time to renew the token (plus some seconds)
                        await Task.Delay(sleepTime, cancellationToken);
                    }
                }
                else
                {
                    if (dateExpiry != null)
                    {
                        // The token is still valid, sleep
                        _managementApiAccessTokenValue = accessToken;
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
