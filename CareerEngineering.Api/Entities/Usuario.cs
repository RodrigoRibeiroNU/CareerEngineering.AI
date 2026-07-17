using System;
using System.Collections.Generic;

namespace CareerEngineering.Api.Entities
{
    public class Usuario
    {
        // O Id será o "sub" string que o Auth0 envia (ex: "google-oauth2|12345...")
        public string Id { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

        // Relacionamento 1:N (Um usuário pode ter várias análises)
        public ICollection<Analise> Analises { get; set; } = new List<Analise>();
    }
}