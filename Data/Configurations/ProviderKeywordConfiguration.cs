using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebApp2.Data.Entities;

namespace WebApp2.Data.Configurations
{
    public class ProviderKeywordConfiguration : IEntityTypeConfiguration<ProviderKeyword>
    {
        public void Configure(EntityTypeBuilder<ProviderKeyword> builder)
        {
            builder.ToTable("PROVIDER_KEYWORD");  // Set table name

            // Set the primary key
            builder.HasKey(k => k.KeywordId);

            // Map KeywordId to KEYWORD_ID
            builder.Property(k => k.KeywordId)
                .HasColumnName("KEYWORD_ID");

            // Map Keyword to KEYWORD, set max length to 100 bytes
            builder.Property(k => k.Keyword)
                .HasColumnName("KEYWORD")
                .HasMaxLength(100)
                .IsRequired();
        }
    }
}