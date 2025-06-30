using Microsoft.EntityFrameworkCore;
using WebApp2.Data.Entities;

namespace WebApp2.Data
{
    public class AppRepository : IAppRepository
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
        public AppRepository(IDbContextFactory<ApplicationDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<IList<ProviderKeyword>> GetKeywords()
        {
            using (var appContext = await _dbContextFactory.CreateDbContextAsync())
            {
                return await appContext.Keywords.AsNoTracking().ToListAsync();
            }
        }

        public async Task<IList<Provider>> GetProviders()
        {
            using (var appContext = await _dbContextFactory.CreateDbContextAsync())
            {
                return await appContext.Providers.Where(x => x.SysDeleted == 0).AsNoTracking().ToListAsync();
            }
        }

        public async Task<IList<ProviderKeywordMapping>> GetProvidersKeywordsMappings()
        {
            using (var appContext = await _dbContextFactory.CreateDbContextAsync())
            {
                return await appContext.KeywordsMapping.AsNoTracking().ToListAsync();
            }
        }
    }
}
