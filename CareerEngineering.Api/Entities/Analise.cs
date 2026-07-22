namespace CareerEngineering.Api.Entities;

/// <summary>
/// Sessão de gap analysis (âncoras de contexto + metadados).
/// Os turnos da conversa ficam em <see cref="MensagemHistorico"/>.
/// </summary>
public class Analise
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UsuarioId { get; set; } = string.Empty;
    public Usuario? Usuario { get; set; }

    public string Titulo { get; set; } = string.Empty;

    /// <summary>Texto completo da vaga — âncora de contexto do sistema.</summary>
    public string DescricaoVaga { get; set; } = string.Empty;

    /// <summary>Texto completo do currículo — âncora de contexto do sistema.</summary>
    public string TextoCurriculo { get; set; } = string.Empty;

    public string ModeloLLM { get; set; } = string.Empty;

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public ICollection<MensagemHistorico> Mensagens { get; set; } = new List<MensagemHistorico>();
}
