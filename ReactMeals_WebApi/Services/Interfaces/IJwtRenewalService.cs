namespace ReactMeals_WebApi.Services.Interfaces;

// Interface that defines JWT renewal operations
public interface IJwtRenewalService : IHostedService, IDisposable
{
    public Task RenewTokenLoop(CancellationToken cancellationToken);
    public string ManagementApiToken { get; set; }
}