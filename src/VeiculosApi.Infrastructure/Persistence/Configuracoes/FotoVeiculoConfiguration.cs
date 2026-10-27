using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Infrastructure.Persistence.Configuracoes;

internal sealed class FotoVeiculoConfiguration : IEntityTypeConfiguration<FotoVeiculo>
{
    public void Configure(EntityTypeBuilder<FotoVeiculo> builder)
    {
        builder.ToTable("fotos_veiculo");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.Property(f => f.ContentType).HasMaxLength(50).IsRequired();
        builder.Property(f => f.Tamanho).IsRequired();
        builder.Property(f => f.ChaveArmazenamento).HasMaxLength(200).IsRequired();
        builder.Property(f => f.CriadoEm).IsRequired();

        builder.HasOne<Veiculo>()
            .WithMany()
            .HasForeignKey(f => f.VeiculoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => f.VeiculoId).HasDatabaseName("ix_fotos_veiculo_veiculo_id");
    }
}
