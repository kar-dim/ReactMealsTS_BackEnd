namespace ReactMeals_WebApi.Services.Interfaces;

// Interface for the tunnel service
public interface ITunnelService : IHostedService, IDisposable
{
    public Task StartTunnelAsync(string localUrl, string tunnelUrl, CancellationToken stoppingToken);
}