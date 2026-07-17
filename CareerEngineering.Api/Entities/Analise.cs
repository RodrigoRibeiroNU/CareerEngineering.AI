using System;

namespace CareerEngineering.Api.Entities
{
    public class Analise
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        // Chave estrangeira ligada ao usuário do Auth0
        public string UsuarioId { get; set; } = string.Empty;
        public Usuario? Usuario { get; set; }

        public string VagaText { get; set; } = string.Empty;
        public string CurriculoText { get; set; } = string.Empty;
        public string Resultado { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    }
}