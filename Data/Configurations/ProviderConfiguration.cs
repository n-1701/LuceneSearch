using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebApp2.Data.Entities;

namespace WebApp2.Data.Configurations;

public class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.ToTable("TPROVIDER_DETAIL");  // Set table name

        // Set the primary key
        builder.HasKey(k => k.HhEntityId);

        builder.Property(e => e.ProviderId)
            .HasColumnName("PROVIDER_ID")
            .HasColumnType("NUMBER(9)");

        builder.Property(e => e.CountryId)
            .HasColumnName("COUNTRY_ID")
            .HasColumnType("NUMBER(9)");

        builder.Property(e => e.HhEntityId)
            .HasColumnName("HH_ENTITY_ID")
            .HasColumnType("NUMBER(15)");

        builder.Property(e => e.HhProvTypeId)
            .HasColumnName("HH_PROV_TYPE_ID")
            .HasColumnType("NUMBER(6)");

        builder.Property(e => e.HhProviderType)
            .HasColumnName("HH_PROVIDER_TYPE")
            .HasMaxLength(400);

        builder.Property(e => e.ProviderName)
            .HasColumnName("PROVIDER_NAME")
            .HasMaxLength(400);

        builder.Property(e => e.ProviderLocLatitude)
            .HasColumnName("PROVIDER_LOC_LATITUDE")
            .HasMaxLength(32);

        builder.Property(e => e.ProviderLocLongitude)
            .HasColumnName("PROVIDER_LOC_LONGITUDE")
            .HasMaxLength(32);

        builder.Property(e => e.ProviderCpe)
            .HasColumnName("PROVIDER_CPE")
            .HasColumnType("NUMBER");

        builder.Property(e => e.NntProviderNetworkId)
            .HasColumnName("NNT_PROVIDER_NETWORK_ID")
            .HasColumnType("CHAR(2)");

        builder.Property(e => e.CignaProviderNetwork)
            .HasColumnName("CIGNA_PROVIDER_NETWORK")
            .HasMaxLength(64);

        builder.Property(e => e.ProviderLocCityId)
            .HasColumnName("PROVIDER_LOC_CITY_ID")
            .HasColumnType("NUMBER(4)");

        builder.Property(e => e.ProviderAddress)
            .HasColumnName("PROVIDER_ADDRESS")
            .HasMaxLength(256);

        builder.Property(e => e.SysDeleted)
            .HasColumnName("SYS_DELETED")
            .HasColumnType("NUMBER");

        builder.Property(e => e.SysInsertDate)
            .HasColumnName("SYS_INSERT_DATE")
            .HasColumnType("DATE");

        builder.Property(e => e.SysRefreshDate)
            .HasColumnName("SYS_REFRESH_DATE")
            .HasColumnType("DATE");
    }
}