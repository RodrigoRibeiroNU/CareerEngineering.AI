using CareerEngineering.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerEngineering.Api.Data.Mappings;

public class MensagemHistoricoMapping : IEntityTypeConfiguration<MensagemHistorico>
{
    public void Configure(EntityTypeBuilder<MensagemHistorico> builder)
    {
        builder.ToTable("MensagensHistorico");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Role)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(m => m.Conteudo)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(m => m.DataEnvio)
            .IsRequired();

        builder.HasIndex(m => new { m.AnaliseId, m.DataEnvio });
    }
}
