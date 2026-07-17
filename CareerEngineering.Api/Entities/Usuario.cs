namespace CareerEngineering.Api.Entities;

public class Usuario
{
    public string Id { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
    public ICollection<Analise> Analises { get; set; } = new List<Analise>();
}
