namespace CareerEngineering.Api.Entities;

/// <summary>
/// Mensagem de um turno da conversa (system / user / assistant).
/// </summary>
public class MensagemHistorico
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AnaliseId { get; set; }
    public Analise? Analise { get; set; }

    /// <summary>Papel no ChatHistory do Semantic Kernel: system, user ou assistant.</summary>
    public string Role { get; set; } = string.Empty;

    public string Conteudo { get; set; } = string.Empty;

    public DateTime DataEnvio { get; set; } = DateTime.UtcNow;
}
