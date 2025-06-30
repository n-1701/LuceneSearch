using WebApp2.Data.Entities;

namespace WebApp2.Models;

public class HealthcareProvider
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; } // Hospital, Clinic
    public List<string> Keywords { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int? ProviderCpe { get; set; }                  
    public string ProviderNetworkId { get; set; }
}

public static class HealthcareProviderMapper
{
    public static List<HealthcareProvider> MapToHealthcareProviders(
        IList<Provider> providers,
        IList<ProviderKeyword> keywords,
        IList<ProviderKeywordMapping> mappings)
    {
        // Create a lookup dictionary for faster keyword access
        var keywordLookup = keywords.ToDictionary(k => k.KeywordId, k => k.Keyword);

        // Group mappings by provider for efficient lookup
        var providerMappings = mappings.GroupBy(
            m => ( m.ProviderId, m.CountryId ),
            m => m.KeywordId
        ).ToDictionary(
            g => g.Key,
            g => g.ToHashSet()
        );

        return providers.Select(provider => MapToHealthcareProvider(
            provider,
            keywordLookup,
            providerMappings
        )).ToList();
    }

    private static HealthcareProvider MapToHealthcareProvider(
        Provider provider,
        Dictionary<int, string> keywordLookup,
        Dictionary<(int ProviderId, int CountryId), HashSet<int>> providerMappings)
    {
        return new HealthcareProvider
        {
            Id = provider.HhEntityId,
            Name = provider.ProviderName,
            Type = provider.HhProviderType,
            Address = provider.ProviderAddress,
            Latitude = ParseGeoLocation(provider.ProviderLocLatitude),
            Longitude = ParseGeoLocation(provider.ProviderLocLongitude),
            Keywords = GetProviderKeywords(
                provider.ProviderId,
                provider.CountryId,
                keywordLookup,
                providerMappings
            ),
            ProviderCpe = provider.ProviderCpe,
            ProviderNetworkId = provider.NntProviderNetworkId
        };
    }

    private static List<string> GetProviderKeywords(
        int providerId,
        int countryId,
        Dictionary<int, string> keywordLookup,
        Dictionary<(int ProviderId, int CountryId), HashSet<int>> providerMappings)
    {
        var key = (ProviderId: providerId, CountryId: countryId);

        if (!providerMappings.TryGetValue(key, out var keywordIds))
        {
            return new List<string>();
        }

        return keywordIds
            .Where(keywordId => keywordLookup.ContainsKey(keywordId))
            .Select(keywordId => keywordLookup[keywordId])
            .ToList();
    }

    private static double ParseGeoLocation(string location)
    {
        return double.TryParse(location, out double result) ? result : 0.0;
    }
}
