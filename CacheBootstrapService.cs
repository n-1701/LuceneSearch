using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using WebApp2.Data;
using WebApp2.Models;
using WebApp2.Services;

namespace WebApp2;

public class CacheBootstrapService : IHostedService
{
    // private readonly IMemoryCache _memoryCache;
    private readonly IAppRepository _appRepository;
    private readonly ILogger<CacheBootstrapService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly ILuceneHProviderService _luceneService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="memoryCache"></param>
    public CacheBootstrapService(ILogger<CacheBootstrapService> logger, IAppRepository appRepository, IMemoryCache memoryCache, ILuceneHProviderService luceneService)
    {
        _logger = logger;
        _appRepository = appRepository;
        _memoryCache = memoryCache;
        _luceneService = luceneService;
    }


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var activity = new Activity("LoadProfessional");
        activity.Start();
        var providers = HealthcareProviderMapper.MapToHealthcareProviders(await _appRepository.GetProviders(), 
            await _appRepository.GetKeywords(), 
            await _appRepository.GetProvidersKeywordsMappings());
        _memoryCache.Set("Providers", providers);
        _luceneService.AddProviders(providers);
        activity.Stop();
        _logger.LogInformation($"Load cache took {activity.Duration} ms to complete the task.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
