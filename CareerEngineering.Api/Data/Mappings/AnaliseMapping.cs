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

        builder.Property(a => a.Titulo)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.DescricaoVaga)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.TextoCurriculo)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.ModeloLLM)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.DataCriacao)
            .IsRequired();

        builder.Property(a => a.UsuarioId)
            .IsRequired()
            .HasMaxLength(150);

        // Índice composto para listagem rápida da Sidebar (por usuário, mais recente primeiro).
        builder.HasIndex(a => new { a.UsuarioId, a.DataCriacao });

        builder.HasMany(a => a.Mensagens)
            .WithOne(m => m.Analise)
            .HasForeignKey(m => m.AnaliseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
