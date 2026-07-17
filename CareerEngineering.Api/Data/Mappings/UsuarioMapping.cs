using CareerEngineering.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerEngineering.Api.Data.Mappings
{
    public class UsuarioMapping : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("Usuarios");

            // O Id do usuário é a string do sub do Auth0 (ex: google-oauth2|...)
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id)
                .HasMaxLength(150)
                .ValueGeneratedNever(); // O ID é gerado externamente pelo Auth0

            builder.Property(u => u.Nome)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(u => u.DataCadastro)
                .IsRequired();

            // Relacionamento 1:N (Um usuário tem muitas análises)
            builder.HasMany(u => u.Analises)
                .WithOne(a => a.Usuario)
                .HasForeignKey(a => a.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade); // Se deletar o usuário, apaga as análises dele
        }
    }
}