namespace CareerEngineering.Api.Data;

// O perfil do usuário, vinculado ao ID do Auth0
public class UserProfile
{
    public int Id { get; set; }
    public string Auth0Id { get; set; } = string.Empty; // Chave vinda do Auth0
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    
    // Relações
    public List<Resume> Resumes { get; set; } = new();
    public List<CareerAnalysis> AnalysisHistory { get; set; } = new();
}

public class Resume
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public int UserProfileId { get; set; }
    public UserProfile User { get; set; } = null!;
}

public class CareerAnalysis
{
    public int Id { get; set; }
    public string JobDescription { get; set; } = string.Empty;
    public string AiResult { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public int UserProfileId { get; set; }
    public UserProfile User { get; set; } = null!;
}