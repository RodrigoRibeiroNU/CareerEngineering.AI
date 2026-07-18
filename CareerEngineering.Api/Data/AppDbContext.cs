using CareerEngineering.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareerEngineering.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Analise> Analises => Set<Analise>();
    public DbSet<MensagemHistorico> MensagensHistorico => Set<MensagemHistorico>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
