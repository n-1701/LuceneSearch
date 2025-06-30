using Microsoft.DotNet.Scaffolding.Shared;
using WebApp2.Data.Entities;

namespace WebApp2.Data
{
    public interface IAppRepository
    {
        Task<IList<Provider>> GetProviders();

        Task<IList<ProviderKeyword>> GetKeywords();

        Task<IList<ProviderKeywordMapping>> GetProvidersKeywordsMappings();

    }
}
