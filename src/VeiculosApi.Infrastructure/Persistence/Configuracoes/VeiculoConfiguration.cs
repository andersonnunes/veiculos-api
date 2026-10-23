using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Infrastructure.Persistence.Configuracoes;

internal sealed class VeiculoConfiguration : IEntityTypeConfiguration<Veiculo>
{
    public void Configure(EntityTypeBuilder<Veiculo> builder)
    {
        builder.ToTable("veiculos");
        builder.Property<uint>("Versao")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();
        builder.Property(v => v.Marca).HasMaxLength(50).IsRequired();
        builder.Property(v => v.Modelo).HasMaxLength(50).IsRequired();
        builder.Property(v => v.Ano).IsRequired();
        builder.Property(v => v.Cor).HasMaxLength(30).IsRequired();
        builder.Property(v => v.Preco).HasPrecision(12, 2).IsRequired();
        builder.Property(v => v.Chassi).HasMaxLength(17).IsRequired();
        builder.Property(v => v.Placa).HasMaxLength(7);
        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(v => v.CriadoEm).IsRequired();

        builder.HasIndex(v => v.Chassi)
            .IsUnique()
            .HasDatabaseName("ux_veiculos_chassi");

        builder.HasIndex(v => v.Placa)
            .IsUnique()
            .HasFilter("\"Placa\" IS NOT NULL")
            .HasDatabaseName("ux_veiculos_placa");
    }
}
