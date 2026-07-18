using CareerEngineering.Api.DTOs;
using CareerEngineering.Api.Entities;

namespace CareerEngineering.Api.Services;

public interface IAnaliseService
{
    Task EnsureUsuarioAsync(string usuarioId, string nome, string email);

    Task<Analise> CriarAnaliseAsync(
        string usuarioId,
        string titulo,
        string descricaoVaga,
        string textoCurriculo,
        string modeloLlm);

    Task AdicionarMensagemAsync(Guid analiseId, string role, string conteudo);

    Task<IReadOnlyList<AnaliseListItemDto>> ListarPorUsuarioAsync(string usuarioId);

    Task<AnaliseDetailDto?> ObterDetalheAsync(Guid id, string usuarioId);

    Task<Analise?> ObterEntidadeDoUsuarioAsync(Guid id, string usuarioId);

    /// <summary>
    /// Sliding window: últimas N mensagens ordenadas cronologicamente (para o ChatHistory).
    /// </summary>
    Task<IReadOnlyList<MensagemHistorico>> ObterUltimasMensagensAsync(Guid analiseId, int take);

    Task<bool> AtualizarTituloAsync(Guid id, string usuarioId, string novoTitulo);

    Task<bool> ExcluirAsync(Guid id, string usuarioId);
}
