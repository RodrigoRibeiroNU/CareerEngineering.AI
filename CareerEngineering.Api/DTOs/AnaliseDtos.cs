namespace CareerEngineering.Api.DTOs;

public record AnaliseListItemDto(
    Guid Id,
    string Titulo,
    DateTime DataCriacao,
    string ModeloLLM);

public record MensagemHistoricoDto(
    Guid Id,
    string Role,
    string Conteudo,
    DateTime DataEnvio);

public record AnaliseDetailDto(
    Guid Id,
    string Titulo,
    string DescricaoVaga,
    string TextoCurriculo,
    string ModeloLLM,
    DateTime DataCriacao,
    IReadOnlyList<MensagemHistoricoDto> Mensagens);

public record UpdateTituloDto(string Titulo);
