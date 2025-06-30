//using Lucene.Net.Analysis.Standard;
//using Lucene.Net.Documents;
//using Lucene.Net.Index;
//using Lucene.Net.Spatial.Vector;
//using Lucene.Net.Spatial;
//using Lucene.Net.Store;
//using WebApp2.Models;

//namespace WebApp2.Services.ProviderStore;

//public class LuceneIndexService
//{
//    private readonly FSDirectory _directory;
//    private readonly StandardAnalyzer _analyzer;
//    private readonly IndexWriter _writer;
//    private readonly Spatial4n.Context.SpatialContext _spatialContext;
//    private readonly SpatialStrategy _vectorStrategy;

//    public LuceneIndexService(string indexPath)
//    {
//        _directory = FSDirectory.Open(indexPath);
//        _analyzer = new StandardAnalyzer(LuceneConstants.APP_LUCENE_VERSION);

//        var indexConfig = new IndexWriterConfig(LuceneConstants.APP_LUCENE_VERSION, _analyzer)
//        {
//            OpenMode = OpenMode.CREATE_OR_APPEND
//        };

//        _writer = new IndexWriter(_directory, indexConfig);
//        _spatialContext = Spatial4n.Context.SpatialContext.Geo;
//        _vectorStrategy = new PointVectorStrategy(_spatialContext, LuceneConstants.FIELD_LOCATION);
//    }

//    public void AddProvider(HealthcareProvider provider)
//    {
//        var doc = CreateDocument(provider);
//        _writer.UpdateDocument(new Term(LuceneConstants.FIELD_ID, provider.Id.ToString()), doc);
//    }

//    public void AddProviders(IEnumerable<HealthcareProvider> providers)
//    {
//        foreach (var provider in providers)
//        {
//            AddProvider(provider);
//        }
//        CommitChanges();
//    }

//    public void DeleteProvider(string id)
//    {
//        _writer.DeleteDocuments(new Term(LuceneConstants.FIELD_ID, id));
//        CommitChanges();
//    }

//    public void CommitChanges()
//    {
//        _writer.Commit();
//    }

//    private Document CreateDocument(HealthcareProvider provider)
//    {
//        var doc = new Document
//        {
//            // Basic fields
//            new Int64Field(LuceneConstants.FIELD_ID, provider.Id, Field.Store.YES),
//            new TextField(LuceneConstants.FIELD_NAME, provider.Name.ToLowerInvariant(), Field.Store.YES),
//            new StringField(LuceneConstants.FIELD_TYPE, provider.Type, Field.Store.YES),
//            new TextField(LuceneConstants.FIELD_ADDRESS, (provider.Address ?? string.Empty).ToLowerInvariant(), Field.Store.YES),
//            new DoubleField(LuceneConstants.FIELD_LATITUDE, provider.Latitude, Field.Store.YES),
//            new DoubleField(LuceneConstants.FIELD_LONGITUDE, provider.Longitude, Field.Store.YES),
//            new Int32Field(LuceneConstants.FIELD_PROVIDER_CPE, provider.ProviderCpe ?? int.MaxValue, Field.Store.YES),
//            new StringField(LuceneConstants.FIELD_PROVIDER_NETWORK_ID, provider.ProviderNetworkId ?? string.Empty, Field.Store.YES)
//        };

//        // Add keywords
//        foreach (var keyword in provider.Keywords)
//        {
//            doc.Add(new TextField(LuceneConstants.FIELD_KEYWORD, keyword?.ToLower() ?? string.Empty, Field.Store.YES));
//        }

//        // Add spatial data
//        if (IsValidCoordinate(provider.Latitude, provider.Longitude))
//        {
//            var location = _spatialContext.MakePoint(provider.Longitude, provider.Longitude);
//            foreach (var field in _vectorStrategy.CreateIndexableFields(location))
//            {
//                doc.Add(field);
//            }
//        }

//        // Add combined text field
//        var allText = $"{provider.Name} {provider.Type} {string.Join(" ", provider.Keywords)} {provider.Address}";
//        doc.Add(new TextField(LuceneConstants.FIELD_ALL_TEXT, allText.ToLower(), Field.Store.YES));

//        return doc;
//    }

//    private bool IsValidCoordinate(double? latitude, double? longitude)
//    {
//        return latitude.HasValue
//            && longitude.HasValue
//            && latitude.Value >= LuceneConstants.MIN_LATITUDE
//            && latitude.Value <= LuceneConstants.MAX_LATITUDE
//            && longitude.Value >= LuceneConstants.MIN_LONGITUDE
//            && longitude.Value <= LuceneConstants.MAX_LONGITUDE;
//    }
//}