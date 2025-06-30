using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Spatial.Vector;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Spatial4n.Distance;
using WebApp2.Models;

namespace WebApp2.Services
{
    public class LuceneHProviderService : ILuceneHProviderService
    {
        // Lucene configuration constants
        private const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;

        // Fields for Lucene components
        private readonly FSDirectory _directory;
        private readonly StandardAnalyzer _analyzer;
        private readonly IndexWriter _writer;
        private readonly SearcherManager _searcherManager;
        private readonly Spatial4n.Context.SpatialContext _spatialContext;
        private readonly SpatialStrategy _vectorStrategy;

        public LuceneHProviderService(string indexPath)
        {
            // Ensure indexPath is valid
            if (string.IsNullOrWhiteSpace(indexPath))
            {
                throw new ArgumentException("Index path cannot be null or empty.", nameof(indexPath));
            }

            // Initialize directory
            _directory = FSDirectory.Open(indexPath);

            // Initialize analyzer
            _analyzer = new StandardAnalyzer(AppLuceneVersion);

            // Configure IndexWriter
            var indexConfig = new IndexWriterConfig(AppLuceneVersion, _analyzer)
            {
                OpenMode = OpenMode.CREATE_OR_APPEND
            };

            // Create IndexWriter
            _writer = new IndexWriter(_directory, indexConfig);

            // Initialize SearcherManager for managing searchers
            _searcherManager = new SearcherManager(_writer, applyAllDeletes: true, null);

            // Initialize spatial context and strategy
            _spatialContext = Spatial4n.Context.SpatialContext.Geo;
            _vectorStrategy = new PointVectorStrategy(_spatialContext, "location");
        }

        public void AddProviders(IEnumerable<HealthcareProvider> providers)
        {
            foreach (var provider in providers)
            {
                AddProvider(provider);
            }
            UpdateIndex();
        }

        public void AddProvider(HealthcareProvider provider)
        {
            var doc = new Document();

            // Basic fields
            doc.Add(new Int64Field("id", provider.Id, Field.Store.YES));
            doc.Add(new TextField("name", provider.Name.ToLowerInvariant(), Field.Store.YES));
            doc.Add(new StringField("type", provider.Type, Field.Store.YES));
            doc.Add(new TextField("address", (provider.Address ?? string.Empty).ToLowerInvariant(), Field.Store.YES));
            doc.Add(new DoubleField("latitude", provider.Latitude, Field.Store.YES));
            doc.Add(new DoubleField("longitude", provider.Longitude, Field.Store.YES));
            doc.Add(new Int32Field("providercpe", provider.ProviderCpe ?? int.MaxValue, Field.Store.YES));
            doc.Add(new StringField("providernetworkid", provider.ProviderNetworkId ?? string.Empty, Field.Store.YES));
            foreach (var keyword in provider.Keywords)
            {
                doc.Add(new TextField("keyword", keyword?.ToLower() ?? string.Empty, Field.Store.YES));
            }
            if (IsValidCoordinate(provider.Latitude, provider.Longitude))
            {
                var location = _spatialContext.MakePoint(provider.Longitude, provider.Latitude);
                foreach (var field in _vectorStrategy.CreateIndexableFields(location))
                {
                    doc.Add(field);
                }
            }
            // Combined field for general search
            var allText = $"{provider.Name} {provider.Type} {string.Join(" ", provider.Keywords)} {provider.Address}";
            doc.Add(new TextField("alltext", allText.ToLower() ?? string.Empty, Field.Store.YES));

            _writer.UpdateDocument(new Term("id", provider.Id.ToString()), doc);
        }

        public void DeleteProvider(string id)
        {
            _writer.DeleteDocuments(new Term("id", id));
            UpdateIndex();
        }

        public void UpdateIndex()
        {
            _writer.Commit();
            _searcherManager.MaybeRefresh();
        }

        public void ViewAllDocuments()
        {
            var searcher = _searcherManager.Acquire();
            //var searcher = new IndexSearcher(_reader);
            var query = new MatchAllDocsQuery();
            var hits = searcher.Search(query, int.MaxValue).ScoreDocs;    

            foreach (var hit in hits)
            {
                var doc = searcher.Doc(hit.Doc);                
            }
        }

        public LuceneSearchResponse Search(LuceneSearchRequest request)
        {
            _searcherManager.MaybeRefresh();
            var searcher = _searcherManager.Acquire();
            var response = new LuceneSearchResponse();

            try
            {
                // Exact keyword matching for short queries
                if (request.Query.Length <= 6)
                {
                    response.ExactKeywordMatches = SearchExactKeywords(searcher, request.Query.ToLower(), request.MaxResults);

                    if (response.ExactKeywordMatches.Any())
                    {
                        response.MatchType = "Keywords";
                    }
                }

                // Construct the BooleanQuery
                var boolQuery = BuildBooleanQuery(request);

                // Apply geolocation filter
                if (request.Latitude.HasValue && request.Longitude.HasValue)
                {
                    AddGeolocationFilter(boolQuery, request.Latitude.Value, request.Longitude.Value, request.RadiusKm);
                }

                // Execute the search
                var topDocs = searcher.Search(boolQuery, request.MaxResults);
                response.ProviderMatches = ProcessSearchResults(searcher, topDocs, request);

                return response;
            }
            finally
            {
                // Release the searcher to prevent resource leaks
                _searcherManager.Release(searcher);
            }
        }

        // Constructs the BooleanQuery with field boosts and wildcard patterns
        private BooleanQuery BuildBooleanQuery(LuceneSearchRequest request)
        {
            var boolQuery = new BooleanQuery();
            var analyzer = new StandardAnalyzer(AppLuceneVersion);

            if (!string.IsNullOrEmpty(request.Query))
            {
                var words = request.Query.ToLowerInvariant().Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var fieldBoost in request.FieldBoosts)
                {
                    // Parse the main query
                    var parser = new QueryParser(AppLuceneVersion, fieldBoost.Key, analyzer);
                    var mainQuery = parser.Parse(request.Query.ToLowerInvariant());
                    mainQuery.Boost = fieldBoost.Value;
                    boolQuery.Add(mainQuery, Occur.SHOULD);

                    // Add wildcard variations for each word
                    AddWildcardQueries(boolQuery, fieldBoost.Key, words);
                }
            }

            return boolQuery;
        }

        // Adds wildcard queries for each word with decreasing boosts
        private void AddWildcardQueries(BooleanQuery boolQuery, string field, string[] words)
        {
            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i];
                var boostFactor = 1.5f - (i / 100.0f);

                // Add variations of wildcard queries
                AddWildcardQuery(boolQuery, field, word, boostFactor);
                AddWildcardQuery(boolQuery, field, $"{word}*", boostFactor - 0.01f);
                AddWildcardQuery(boolQuery, field, $"*{word}", boostFactor - 0.03f);
                AddWildcardQuery(boolQuery, field, $"*{word}*", boostFactor - 0.04f);
            }
        }

        // Adds a single wildcard query
        private void AddWildcardQuery(BooleanQuery boolQuery, string field, string pattern, float boost)
        {
            var query = new WildcardQuery(new Term(field, pattern)) { Boost = boost };
            boolQuery.Add(query, Occur.SHOULD);
        }

        // Adds a geolocation filter to the query
        private void AddGeolocationFilter(BooleanQuery boolQuery, double latitude, double longitude, double radiusKm)
        {
            var center = _spatialContext.MakePoint(longitude, latitude);
            var circle = _spatialContext.MakeCircle(center, DistanceUtils.Dist2Degrees(radiusKm, DistanceUtils.EarthMeanRadiusKilometers));
            var spatialQuery = new SpatialArgs(SpatialOperation.Intersects, circle);
            var spatialFilter = _vectorStrategy.MakeQuery(spatialQuery);

            boolQuery.Add(spatialFilter, Occur.MUST);
        }

        // Processes search results and enriches them with metadata
        private List<LuceneSearchResult> ProcessSearchResults(IndexSearcher searcher, TopDocs topDocs, LuceneSearchRequest request)
        {
            var results = new List<LuceneSearchResult>();
            var seenProviders = new HashSet<string>();

            foreach (var scoreDoc in topDocs.ScoreDocs)
            {
                var doc = searcher.Doc(scoreDoc.Doc);

                // Avoid duplicate providers
                if (seenProviders.Contains(doc.Get("id"))) continue;
                seenProviders.Add(doc.Get("id"));

                // Parse metadata
                var latitude = double.Parse(doc.Get("latitude"));
                var longitude = double.Parse(doc.Get("longitude"));

                // Calculate distance if location is provided
                double? distanceKm = request.Latitude.HasValue && request.Longitude.HasValue
                    ? CalculateDistance(request.Latitude.Value, request.Longitude.Value, latitude, longitude)
                    : null;

                results.Add(new LuceneSearchResult
                {
                    Provider = new HealthcareProvider
                    {
                        Id = long.Parse(doc.Get("id")),
                        Name = doc.Get("name"),
                        Type = doc.Get("type"),
                        Address = doc.Get("address"),
                        Keywords = doc.GetFields("keyword").Select(f => f.GetStringValue()).ToList(),
                        Latitude = latitude,
                        Longitude = longitude,
                        ProviderCpe = int.Parse(doc.Get("providercpe")),
                        ProviderNetworkId = doc.Get("providernetworkid")
                    },
                    Score = scoreDoc.Score,
                    DistanceKm = distanceKm,
                    MatchedFields = GetMatchedFields(searcher, scoreDoc.Doc, request.Query)
                });
            }

            // Sort results
            return results
                .OrderByDescending(r => r.Score)
                .ThenBy(r => r.Provider.ProviderCpe)
                .ThenBy(r => r.DistanceKm)
                .ToList();
        }


        //public LuceneSearchResponse  Search(LuceneSearchRequest request)
        //{
        //    _searcherManager.MaybeRefresh();
        //    var searcher = _searcherManager.Acquire();
        //    var response = new LuceneSearchResponse();

        //    try
        //    {
        //        if (request.Query.Length <= 6)
        //        {
        //            var exactKeywords = SearchExactKeywords(searcher, request.Query.ToLower(), request.MaxResults);
        //            response.ExactKeywordMatches = exactKeywords;

        //            if (exactKeywords.Any())
        //            {
        //                response.MatchType = "Keywords";
        //                //return response;
        //            }
        //        }

        //        var boolQuery = new BooleanQuery();
        //        var standardAnalyzer = new StandardAnalyzer(AppLuceneVersion);
        //        // Text search with boosts and fuzzy matching
        //        if (!string.IsNullOrEmpty(request.Query))
        //        {
        //            var words = request.Query.ToLowerInvariant().Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        //            //var wildcardPattern = "*" + string.Join("*", words) + "*";
        //            foreach (var fieldBoost in request.FieldBoosts)
        //            {
        //                QueryParser parser = new QueryParser(AppLuceneVersion, fieldBoost.Key, standardAnalyzer);
        //                Query query = parser.Parse(request.Query.ToLowerInvariant());
        //                query.Boost = fieldBoost.Value;
        //                boolQuery.Add(query, Occur.SHOULD);

        //                int wordIndex = 0;
        //               foreach(var word in words)
        //                {
        //                    Query fieldQuery;
        //                    fieldQuery = new WildcardQuery(new Term(fieldBoost.Key, $"{word}"));
        //                    fieldQuery.Boost = 1.5f - ((wordIndex)/100);
        //                    boolQuery.Add(fieldQuery, Occur.SHOULD);
        //                    fieldQuery = new WildcardQuery(new Term(fieldBoost.Key, $"{word}*"));
        //                    fieldQuery.Boost = 1.5f - ((wordIndex +1)/100);
        //                    boolQuery.Add(fieldQuery, Occur.SHOULD);
        //                    fieldQuery = new WildcardQuery(new Term(fieldBoost.Key, $"* {word}"));
        //                    fieldQuery.Boost = 1.5f - ((wordIndex + 3)/100);
        //                    boolQuery.Add(fieldQuery, Occur.SHOULD);
        //                    fieldQuery = new WildcardQuery(new Term(fieldBoost.Key, $"* {word}*"));
        //                    fieldQuery.Boost = 1.5f - ((wordIndex + 4)/100);
        //                    boolQuery.Add(fieldQuery, Occur.SHOULD);
        //                    wordIndex += 1;
        //                }
        //            }
        //        }

        //        // Geolocation filter
        //        if (request.Latitude.HasValue && request.Longitude.HasValue)
        //        {
        //            var center = _spatialContext.MakePoint(request.Longitude.Value, request.Latitude.Value);
        //            var circle = _spatialContext.MakeCircle(center, DistanceUtils.Dist2Degrees(request.RadiusKm, DistanceUtils.EarthMeanRadiusKilometers));
        //            var spatialQuery = new SpatialArgs(SpatialOperation.Intersects, circle);
        //            var spatialFilter = _vectorStrategy.MakeQuery(spatialQuery);
        //            boolQuery.Add(spatialFilter, Occur.MUST);
        //        }
        //       // var sortFields = new List<SortField>();
        //        //sortFields.Add(new SortField("name", SortFieldType.STRING, false));

        //        var topDocs = searcher.Search(boolQuery, request.MaxResults);
        //        var results = new List<LuceneSearchResult>();
        //        var seenProviders = new HashSet<string>();
        //        foreach (var scoreDoc in topDocs.ScoreDocs)
        //        {
        //            var doc = searcher.Doc(scoreDoc.Doc);
        //            if (seenProviders.Contains(doc.Get("id"))) continue;
        //            seenProviders.Add(doc.Get("id"));
        //            var latitude = double.Parse(doc.Get("latitude"));
        //            var longitude = double.Parse(doc.Get("longitude"));

        //            double? distanceKm = null;
        //            if (request.Latitude.HasValue && request.Longitude.HasValue)
        //            {
        //                distanceKm = CalculateDistance(
        //                    request.Latitude.Value, request.Longitude.Value,
        //                    latitude, longitude);
        //            }

        //            results.Add(new LuceneSearchResult
        //            {
        //                Provider = new HealthcareProvider
        //                {
        //                    Id = long.Parse(doc.Get("id")),
        //                    Name = doc.Get("name"),
        //                    Type = doc.Get("type"),
        //                    Address = doc.Get("address"),
        //                    Keywords = doc.GetFields("keyword")
        //                                .Select(f => f.GetStringValue())
        //                                .ToList(),
        //                    Latitude = latitude,
        //                    Longitude = longitude,
        //                    ProviderCpe = int.Parse(doc.Get("providercpe")),
        //                    ProviderNetworkId = doc.Get("providernetworkid")
        //                },
        //                Score = scoreDoc.Score,
        //                DistanceKm = distanceKm ,
        //                MatchedFields = GetMatchedFields(searcher, scoreDoc.Doc, request.Query)
        //            });
        //        }
        //        var sortedresult = results.OrderByDescending(x => x.Score)
        //            .ThenBy(x => x.Provider.ProviderCpe).ThenBy(x => x.DistanceKm).ToList();
        //        response.ProviderMatches = sortedresult;
        //        // Sort by distance if location is provided
        //        //if (request.Latitude.HasValue && request.Longitude.HasValue)
        //        //{
        //        //    results = results.OrderBy(r => r.DistanceKm).ToList();
        //        //}

        //        return response;
        //    }
        //    finally
        //    {
        //        _searcherManager.Release(searcher);
        //    }
        //}


        private bool IsValidCoordinate(double? latitude, double? longitude)
        {
            return latitude.HasValue
                && longitude.HasValue
                && latitude.Value >= -90
                && latitude.Value <= 90
                && longitude.Value >= -180
                && longitude.Value <= 180;
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var p1 = _spatialContext.MakePoint(lon1, lat1);
            var p2 = _spatialContext.MakePoint(lon2, lat2);

            return DistanceUtils.Degrees2Dist(
                _spatialContext.DistanceCalculator.Distance(p1, p2),
                DistanceUtils.EarthMeanRadiusKilometers);
        }

        private List<string> SearchExactKeywords(IndexSearcher searcher, string searchTerm, int maxResults)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentException("Search term cannot be null or empty.", nameof(searchTerm));
            }

            // Build wildcard query for "keyword" field
            var query = new WildcardQuery(new Term("keyword", $"{searchTerm}*"));

            // Execute search
            var topDocs = searcher.Search(query, maxResults);

            // Extract unique keywords from results
            var uniqueKeywords = new HashSet<string>();
            foreach (var scoreDoc in topDocs.ScoreDocs)
            {
                var doc = searcher.Doc(scoreDoc.Doc);

                // Collect unique keywords from the document
                foreach (var field in doc.GetFields("keyword"))
                {
                    uniqueKeywords.Add(field.GetStringValue());
                }
            }

            return uniqueKeywords.ToList();
        }



        //private List<string> SearchExactKeywords(IndexSearcher searcher, string searchTerm, int maxResults)
        //{
        //    var query = new WildcardQuery(new Term("keyword", $"{searchTerm}*"));
        //    var topDocs = searcher.Search(query, maxResults);

        //    var uniqueKeywords = new HashSet<string>();
        //    foreach (var scoreDoc in topDocs.ScoreDocs)
        //    {
        //        var doc = searcher.Doc(scoreDoc.Doc);
        //        var keywords = doc.GetFields("keyword")
        //                        .Select(f => f.GetStringValue());

        //        foreach (var keyword in keywords)
        //        {
        //            uniqueKeywords.Add(keyword);
        //        }
        //    }

        //    return uniqueKeywords.ToList();
        //}

        private string[] GetMatchedFields(IndexSearcher searcher, int docId, string searchTerm)
        {
            var matchedFields = new HashSet<string>();
            var fields = new[] { "name", "keyword", "address" };

            foreach (var field in fields)
            {
                var termVector = searcher.IndexReader.GetTermVector(docId, field);
                if (termVector != null)
                {
                    var termsEnum = termVector.GetEnumerator();
                    while (termsEnum.MoveNext())
                    {
                        if (termsEnum.Term.Utf8ToString().Contains(searchTerm.ToLower()))
                        {
                            matchedFields.Add(field);
                            break;
                        }
                    }
                }
            }

            return matchedFields.ToArray();
        }
    }
}
