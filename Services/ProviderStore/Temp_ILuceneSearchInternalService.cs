//using Lucene.Net.Search;

//namespace WebApp2.Services.ProviderStore
//{
//    internal interface ILuceneSearchInternalService
//    {
//        double CalculateDistance(double lat1, double lon1, double lat2, double lon2);
//        BooleanQuery CalculateDistance(LuceneSearchRequest request);
//        List<LuceneSearchResult> ExecuteSearch(IndexSearcher searcher, BooleanQuery boolQuery, LuceneSearchRequest request);
//        string[] GetMatchedFields(IndexSearcher searcher, int docId, string searchTerm);
//        LuceneSearchResponse Search(LuceneSearchRequest request);
//        List<string> SearchExactKeywords(IndexSearcher searcher, string searchTerm, int maxResults);
//    }
//}