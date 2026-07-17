using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CareerEngineering.Api.Data;
using CareerEngineering.Api.Entities;

namespace CareerEngineering.Api.Services
{
    public class CareerMentorService : ICareerMentorService
    {
        private readonly AppDbContext _context;

        public CareerMentorService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Analise> SalvarAnaliseAsync(
            string usuarioId, 
            string nome, 
            string email, 
            string vaga, 
            string curriculo, 
            string resultado)
        {
            // 1. Garantir que o usuário existe no nosso banco local
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
            
            if (usuario == null)
            {
                usuario = new Usuario
                {
                    Id = usuarioId,
                    Nome = nome,
                    Email = email,
                    DataCadastro = DateTime.UtcNow
                };
                
                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();
            }

            // 2. Criar e salvar a nova análise vinculada ao usuário
            var novaAnalise = new Analise
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuario.Id,
                VagaText = vaga,
                CurriculoText = curriculo,
                Resultado = resultado,
                DataCriacao = DateTime.UtcNow
            };

            _context.Analises.Add(novaAnalise);
            await _context.SaveChangesAsync();

            return novaAnalise;
        }
    }
}