using CareerEngineering.Api.Data;
using CareerEngineering.Api.DTOs;
using CareerEngineering.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareerEngineering.Api.Services;

public class AnaliseService : IAnaliseService
{
    private readonly AppDbContext _context;

    public AnaliseService(AppDbContext context)
    {
        _context = context;
    }

    public async Task EnsureUsuarioAsync(string usuarioId, string nome, string email)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario is not null) return;

        _context.Usuarios.Add(new Usuario
        {
            Id = usuarioId,
            Nome = string.IsNullOrWhiteSpace(nome) ? "Usuário" : nome,
            Email = email ?? string.Empty,
            DataCadastro = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    public async Task<Analise> CriarAnaliseAsync(
        string usuarioId,
        string titulo,
        string descricaoVaga,
        string textoCurriculo,
        string modeloLlm)
    {
        var analise = new Analise
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Titulo = Truncate(titulo, 150),
            DescricaoVaga = descricaoVaga,
            TextoCurriculo = textoCurriculo,
            ModeloLLM = Truncate(modeloLlm, 100),
            DataCriacao = DateTime.UtcNow
        };

        _context.Analises.Add(analise);
        await _context.SaveChangesAsync();
        return analise;
    }

    public async Task AdicionarMensagemAsync(Guid analiseId, string role, string conteudo)
    {
        _context.MensagensHistorico.Add(new MensagemHistorico
        {
            Id = Guid.NewGuid(),
            AnaliseId = analiseId,
            Role = role,
            Conteudo = conteudo,
            DataEnvio = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AnaliseListItemDto>> ListarPorUsuarioAsync(string usuarioId)
    {
        return await _context.Analises
            .AsNoTracking()
            .Where(a => a.UsuarioId == usuarioId)
            .OrderByDescending(a => a.DataCriacao)
            .Select(a => new AnaliseListItemDto(a.Id, a.Titulo, a.DataCriacao, a.ModeloLLM))
            .ToListAsync();
    }

    public async Task<AnaliseDetailDto?> ObterDetalheAsync(Guid id, string usuarioId)
    {
        var analise = await _context.Analises
            .AsNoTracking()
            .Where(a => a.Id == id && a.UsuarioId == usuarioId)
            .Select(a => new
            {
                a.Id,
                a.Titulo,
                a.DescricaoVaga,
                a.TextoCurriculo,
                a.ModeloLLM,
                a.DataCriacao,
                Mensagens = a.Mensagens
                    .OrderBy(m => m.DataEnvio)
                    .Select(m => new MensagemHistoricoDto(m.Id, m.Role, m.Conteudo, m.DataEnvio))
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (analise is null) return null;

        return new AnaliseDetailDto(
            analise.Id,
            analise.Titulo,
            analise.DescricaoVaga,
            analise.TextoCurriculo,
            analise.ModeloLLM,
            analise.DataCriacao,
            analise.Mensagens);
    }

    public async Task<Analise?> ObterEntidadeDoUsuarioAsync(Guid id, string usuarioId)
    {
        return await _context.Analises
            .FirstOrDefaultAsync(a => a.Id == id && a.UsuarioId == usuarioId);
    }

    public async Task<IReadOnlyList<MensagemHistorico>> ObterUltimasMensagensAsync(Guid analiseId, int take)
    {
        // Busca as N mais recentes e devolve em ordem cronológica (necessária para o ChatHistory).
        var recentes = await _context.MensagensHistorico
            .AsNoTracking()
            .Where(m => m.AnaliseId == analiseId)
            .OrderByDescending(m => m.DataEnvio)
            .Take(take)
            .ToListAsync();

        recentes.Reverse();
        return recentes;
    }

    public Task<int> ContarMensagensAsync(Guid analiseId) =>
        _context.MensagensHistorico.CountAsync(m => m.AnaliseId == analiseId);

    public async Task<bool> AtualizarTituloAsync(Guid id, string usuarioId, string novoTitulo)
    {
        var analise = await ObterEntidadeDoUsuarioAsync(id, usuarioId);
        if (analise is null) return false;

        analise.Titulo = Truncate(novoTitulo.Trim(), 150);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExcluirAsync(Guid id, string usuarioId)
    {
        var analise = await ObterEntidadeDoUsuarioAsync(id, usuarioId);
        if (analise is null) return false;

        _context.Analises.Remove(analise);
        await _context.SaveChangesAsync();
        return true;
    }

    private static string Truncate(string value, int max)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= max ? value : value[..max];
    }
}
