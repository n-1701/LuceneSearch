using System.Xml;
using WebApp2.Models;
using static Lucene.Net.Util.Packed.PackedInt32s;

namespace WebApp2.Services
{
    public interface ILuceneHProviderService
    {
        void AddProvider(HealthcareProvider provider);
        void AddProviders(IEnumerable<HealthcareProvider> providers);
        void DeleteProvider(string id);
        void UpdateIndex();
        void ViewAllDocuments();
        //LuceneSearchResponse Search(string searchTerm, int maxResults = 10);
        LuceneSearchResponse Search(LuceneSearchRequest request);
    }

    // Models/SearchRequest.cs
    public class LuceneSearchRequest
    {
        public string Query { get; set; }
        public int MaxResults { get; set; } = 25;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double RadiusKm { get; set; } = 40; // Default radius of 40km
        public bool UseFuzzySearch { get; set; } = true;
        public int MaxEdits { get; set; } = 2;
        public int PrefixLength { get; set; } = 0; 

        //4.0f - Very high importance
        //2.0f - High importance
        //1.5f - Medium importance
        //1.0f - Normal importance(default)
        //0.5f - Lower importance
        public Dictionary<string, float> FieldBoosts { get; set; } = new()
            {
                { "name", 4.0f },
                { "keyword", 2.0f },
                { "type", 1.0f }
            };
    }

    // Models/SearchResult.cs
    public class LuceneSearchResult
    {
        public HealthcareProvider Provider { get; set; }
        public double? DistanceKm { get; set; }
        public float Score { get; set; }
        public string[] MatchedFields { get; set; }
    }

    public class LuceneSearchResponse
    {
        public List<string> ExactKeywordMatches { get; set; } = new();
        public List<LuceneSearchResult> ProviderMatches { get; set; } = new();
        public string MatchType { get; set; } // "Keywords" or "Providers"
    }
}
