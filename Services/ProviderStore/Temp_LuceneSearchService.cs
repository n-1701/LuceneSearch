//using Lucene.Net.Analysis.Standard;
//using Lucene.Net.Index;
//using Lucene.Net.Search;
//using Lucene.Net.Spatial.Vector;
//using Lucene.Net.Spatial;
//using Lucene.Net.Store;
//using Lucene.Net.Spatial.Queries;
//using Spatial4n.Distance;
//using Lucene.Net.QueryParsers.Classic;
//using WebApp2.Models;

//namespace WebApp2.Services.ProviderStore;

//public class LuceneSearchService : ILuceneSearchInternalService , ILuceneSearchService
//{
//    private readonly FSDirectory _directory;
//    private readonly StandardAnalyzer _analyzer;
//    private readonly SearcherManager _searcherManager;
//    private readonly Spatial4n.Context.SpatialContext _spatialContext;
//    private readonly SpatialStrategy _vectorStrategy;

//    public LuceneSearchService(string indexPath)
//    {
//        _directory = FSDirectory.Open(indexPath);
//        _analyzer = new StandardAnalyzer(LuceneConstants.APP_LUCENE_VERSION);
//        var indexWriter = new IndexWriter(_directory,
//            new IndexWriterConfig(LuceneConstants.APP_LUCENE_VERSION, _analyzer));
//        _searcherManager = new SearcherManager(indexWriter, true, null);
//        _spatialContext = Spatial4n.Context.SpatialContext.Geo;
//        _vectorStrategy = new PointVectorStrategy(_spatialContext, LuceneConstants.FIELD_LOCATION);
//    }

//    public LuceneSearchResponse Search(LuceneSearchRequest request)
//    {
//        _searcherManager.MaybeRefresh();
//        var searcher = _searcherManager.Acquire();

//        try
//        {
//            var response = new LuceneSearchResponse();

//            // Check for exact keyword matches
//            if (request.Query.Length <= LuceneConstants.MINIMUM_QUERY_LENGTH)
//            {
//                var exactKeywords = SearchExactKeywords(searcher, request.Query.ToLower(), request.MaxResults);
//                response.ExactKeywordMatches = exactKeywords;

//                if (exactKeywords.Any())
//                {
//                    response.MatchType = "Keywords";
//                }
//            }

//            var boolQuery = CreateBooleanQuery(request);
//            var results = ExecuteSearch(searcher, boolQuery, request);

//            response.ProviderMatches = results
//                .OrderByDescending(x => x.Score)
//                .ThenBy(x => x.Provider.ProviderCpe)
//                .ThenBy(x => x.DistanceKm)
//                .ToList();

//            return response;
//        }
//        finally
//        {
//            _searcherManager.Release(searcher);
//        }
//    }

//    public List<LuceneSearchResult> ExecuteSearch(IndexSearcher searcher, BooleanQuery boolQuery, LuceneSearchRequest request)
//    {
//        var topDocs = searcher.Search(boolQuery, request.MaxResults);
//        var results = new List<LuceneSearchResult>();
//        var seenProviders = new HashSet<string>();
//        foreach (var scoreDoc in topDocs.ScoreDocs)
//        {
//            var doc = searcher.Doc(scoreDoc.Doc);
//            if (seenProviders.Contains(doc.Get(LuceneConstants.FIELD_ID))) continue;
//            seenProviders.Add(doc.Get(LuceneConstants.FIELD_ID));
//            var latitude = double.Parse(doc.Get(LuceneConstants.FIELD_LATITUDE));
//            var longitude = double.Parse(doc.Get(LuceneConstants.FIELD_LONGITUDE));

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
//                    Id = long.Parse(doc.Get(LuceneConstants.FIELD_ID)),
//                    Name = doc.Get(LuceneConstants.FIELD_NAME),
//                    Type = doc.Get(LuceneConstants.FIELD_TYPE),
//                    Address = doc.Get(LuceneConstants.FIELD_ADDRESS),
//                    Keywords = doc.GetFields(LuceneConstants.FIELD_KEYWORD)
//                                .Select(f => f.GetStringValue())
//                                .ToList(),
//                    Latitude = latitude,
//                    Longitude = longitude,
//                    ProviderCpe = int.Parse(doc.Get(LuceneConstants.FIELD_PROVIDER_CPE)),
//                    ProviderNetworkId = doc.Get(LuceneConstants.FIELD_PROVIDER_NETWORK_ID)
//                },
//                Score = scoreDoc.Score,
//                DistanceKm = distanceKm,
//                MatchedFields = GetMatchedFields(searcher, scoreDoc.Doc, request.Query)
//            });
//        }

//        return results;
//    }

//    public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
//    {
//        var p1 = _spatialContext.MakePoint(lon1, lat1);
//        var p2 = _spatialContext.MakePoint(lon2, lat2);

//        return DistanceUtils.Degrees2Dist(
//            _spatialContext.DistanceCalculator.Distance(p1, p2),
//            DistanceUtils.EarthMeanRadiusKilometers);
//    }


//    public BooleanQuery CreateBooleanQuery(LuceneSearchRequest request)
//    {
//        var boolQuery = new BooleanQuery();
//        var standardAnalyzer = new StandardAnalyzer(LuceneConstants.APP_LUCENE_VERSION);
//        if (!string.IsNullOrEmpty(request.Query))
//        {
//            var words = request.Query.ToLowerInvariant().Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
//            foreach (var fieldBoost in request.FieldBoosts)
//            {
//                QueryParser parser = new QueryParser(LuceneConstants.APP_LUCENE_VERSION, fieldBoost.Key, standardAnalyzer);
//                Query query = parser.Parse(request.Query.ToLowerInvariant());
//                query.Boost = fieldBoost.Value;
//                boolQuery.Add(query, Occur.SHOULD);

//                int wordIndex = 0;
//                foreach (var word in words)
//                {
//                    Query fieldQuery;
//                    fieldQuery = new WildcardQuery(new Term(fieldBoost.Key, $"{word}"));
//                    fieldQuery.Boost = 1.5f - ((wordIndex) / 100);
//                    boolQuery.Add(fieldQuery, Occur.SHOULD);
//                    fieldQuery = new WildcardQuery(new Term(fieldBoost.Key, $"{word}*"));
//                    fieldQuery.Boost = 1.5f - ((wordIndex + 1) / 100);
//                    boolQuery.Add(fieldQuery, Occur.SHOULD);
//                    fieldQuery = new WildcardQuery(new Term(fieldBoost.Key, $"* {word}"));
//                    fieldQuery.Boost = 1.5f - ((wordIndex + 3) / 100);
//                    boolQuery.Add(fieldQuery, Occur.SHOULD);
//                    fieldQuery = new WildcardQuery(new Term(fieldBoost.Key, $"* {word}*"));
//                    fieldQuery.Boost = 1.5f - ((wordIndex + 4) / 100);
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

//        return boolQuery;
//    }

//    public List<string> SearchExactKeywords(IndexSearcher searcher, string searchTerm, int maxResults)
//    {
//        var query = new WildcardQuery(new Term(LuceneConstants.FIELD_KEYWORD, $"{searchTerm}*"));
//        var topDocs = searcher.Search(query, maxResults);

//        var uniqueKeywords = new HashSet<string>();
//        foreach (var scoreDoc in topDocs.ScoreDocs)
//        {
//            var doc = searcher.Doc(scoreDoc.Doc);
//            var keywords = doc.GetFields(LuceneConstants.FIELD_KEYWORD)
//                            .Select(f => f.GetStringValue());

//            foreach (var keyword in keywords)
//            {
//                uniqueKeywords.Add(keyword);
//            }
//        }

//        return uniqueKeywords.ToList();
//    }


//    public string[] GetMatchedFields(IndexSearcher searcher, int docId, string searchTerm)
//    {
//        var matchedFields = new HashSet<string>();
//        var fields = LuceneConstants.MATCHED_FIELDS;

//        foreach (var field in fields)
//        {
//            var termVector = searcher.IndexReader.GetTermVector(docId, field);
//            if (termVector != null)
//            {
//                var termsEnum = termVector.GetEnumerator();
//                while (termsEnum.MoveNext())
//                {
//                    if (termsEnum.Term.Utf8ToString().Contains(searchTerm.ToLower()))
//                    {
//                        matchedFields.Add(field);
//                        break;
//                    }
//                }
//            }
//        }

//        return matchedFields.ToArray();
//    }
//}