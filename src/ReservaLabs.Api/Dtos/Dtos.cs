using System.ComponentModel.DataAnnotations;
using ReservaLabs.Api.Models;

namespace ReservaLabs.Api.Dtos;

// DTOs como records: imutáveis, concisos e com igualdade por valor.
// As DataAnnotations são validadas automaticamente pelo [ApiController] (resposta 400).

public record LaboratorioResponse(int Id, string Nome, string Bloco, int Capacidade, IReadOnlyList<string> Recursos)
{
    public static LaboratorioResponse De(Laboratorio l) => new(l.Id, l.Nome, l.Bloco, l.Capacidade, l.Recursos);
}

public record CriarReservaRequest(
    [Required(ErrorMessage = "Informe o laboratório")] int? LaboratorioId,
    [Required(ErrorMessage = "Informe o responsável")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O responsável deve ter entre 3 e 100 caracteres")]
    string Responsavel,
    [Required(ErrorMessage = "Informe a disciplina")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "A disciplina deve ter entre 3 e 100 caracteres")]
    string Disciplina,
    [Required(ErrorMessage = "Informe o início")] DateTime? Inicio,
    [Required(ErrorMessage = "Informe o fim")] DateTime? Fim,
    [Range(1, 200, ErrorMessage = "A quantidade de alunos deve estar entre 1 e 200")] int QuantidadeAlunos);

public record ReservaResponse(
    int Id,
    int LaboratorioId,
    string Laboratorio,
    string Responsavel,
    string Disciplina,
    DateTime Inicio,
    DateTime Fim,
    int QuantidadeAlunos,
    string Status);

public record OcupacaoDiaResponse(int LaboratorioId, string Laboratorio, DateOnly Data,
                                  double HorasReservadas, double TaxaOcupacao,
                                  IReadOnlyList<ReservaResponse> Reservas);
