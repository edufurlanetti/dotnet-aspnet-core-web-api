namespace ReservaLabs.Api.Models;

public enum StatusReserva
{
    Ativa,
    Cancelada
}

/// <summary>Reserva de um laboratório para uma aula ou atividade.</summary>
public class Reserva
{
    public int Id { get; set; }
    public int LaboratorioId { get; init; }
    public required string Responsavel { get; init; }
    public required string Disciplina { get; init; }
    public DateTime Inicio { get; init; }
    public DateTime Fim { get; init; }
    public int QuantidadeAlunos { get; init; }
    public StatusReserva Status { get; private set; } = StatusReserva.Ativa;
    public DateTime CriadaEm { get; init; }

    public bool ConflitaCom(DateTime inicio, DateTime fim) =>
        Status == StatusReserva.Ativa && inicio < Fim && fim > Inicio;   // intervalos se sobrepõem

    public void Cancelar() => Status = StatusReserva.Cancelada;
}
