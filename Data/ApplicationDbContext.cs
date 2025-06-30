using Microsoft.EntityFrameworkCore;
using WebApp2.Data.Configurations;
using WebApp2.Data.Entities;

namespace WebApp2.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : base(options)
    {
    }

    public DbSet<ProviderKeywordMapping> KeywordsMapping { get; set; }
    public DbSet<ProviderKeyword> Keywords { get; set; }
    public DbSet<Provider> Providers { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("NTCHBI_T1");
        modelBuilder.ApplyConfiguration(new ProviderConfiguration());
        modelBuilder.ApplyConfiguration(new ProviderKeywordConfiguration());
        modelBuilder.ApplyConfiguration(new ProviderKeywordMappingConfiguration());

    }
}
