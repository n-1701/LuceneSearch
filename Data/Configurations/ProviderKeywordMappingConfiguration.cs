using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebApp2.Data.Entities;

namespace WebApp2.Data.Configurations
{
    internal class ProviderKeywordMappingConfiguration : IEntityTypeConfiguration<ProviderKeywordMapping>
    {
        public void Configure(EntityTypeBuilder<ProviderKeywordMapping> builder)
        {
            builder.ToTable("PROVIDER_KEYWORD_MAPPING");  // Set table name

            // Configure ID with auto-incrementing sequence
            builder.Property(e => e.Id)
                .HasColumnName("ID")
                .HasColumnType("NUMBER");

            // Configure KEYWORD_ID
            builder.Property(e => e.KeywordId)
                .HasColumnName("KEYWORD_ID")
                .HasColumnType("NUMBER")
                .IsRequired();

            // Configure PROVIDER_ID
            builder.Property(e => e.ProviderId)
                .HasColumnName("PROVIDER_ID")
                .HasColumnType("NUMBER")
                .IsRequired();

            // Configure COUNTRY_ID
            builder.Property(e => e.CountryId)
                .HasColumnName("COUNTRY_ID")
                .HasColumnType("NUMBER")
                .IsRequired();

            // Define primary key if needed
            builder.HasKey(e => e.Id);
        }
    }
}