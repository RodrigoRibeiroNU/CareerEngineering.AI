using System.Threading.Tasks;
using CareerEngineering.Api.Entities;

namespace CareerEngineering.Api.Services
{
    public interface ICareerMentorService
    {
        Task<Analise> SalvarAnaliseAsync(
            string usuarioId, 
            string nome, 
            string email, 
            string vaga, 
            string curriculo, 
            string resultado);
    }
}