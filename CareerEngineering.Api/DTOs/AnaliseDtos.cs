namespace CareerEngineering.Api.DTOs;

/// <summary>Item leve para a Sidebar (sem textos pesados de vaga/currículo).</summary>
public record AnaliseListItemDto(
    Guid Id,
    string Titulo,
    DateTime DataCriacao,
    string ModeloLLM);

/// <summary>Mensagem de um turno no histórico.</summary>
public record MensagemHistoricoDto(
    Guid Id,
    string Role,
    string Conteudo,
    DateTime DataEnvio);

/// <summary>Detalhe completo para reabrir um chat antigo.</summary>
public record AnaliseDetailDto(
    Guid Id,
    string Titulo,
    string DescricaoVaga,
    string TextoCurriculo,
    string ModeloLLM,
    DateTime DataCriacao,
    IReadOnlyList<MensagemHistoricoDto> Mensagens);

/// <summary>Payload do PATCH de renomeação.</summary>
public record UpdateTituloDto(string Titulo);
