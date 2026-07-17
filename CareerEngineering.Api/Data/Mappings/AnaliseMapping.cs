using CareerEngineering.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerEngineering.Api.Data.Mappings;

public class AnaliseMapping : IEntityTypeConfiguration<Analise>
{
    public void Configure(EntityTypeBuilder<Analise> builder)
    {
        builder.ToTable("Analises");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.VagaText)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.CurriculoText)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.Resultado)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.DataCriacao)
            .IsRequired();

        builder.Property(a => a.UsuarioId)
            .IsRequired()
            .HasMaxLength(150);
    }
}
