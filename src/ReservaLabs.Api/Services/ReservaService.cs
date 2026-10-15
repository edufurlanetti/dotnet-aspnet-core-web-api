using ReservaLabs.Api.Dtos;
using ReservaLabs.Api.Models;
using ReservaLabs.Api.Repositories;

namespace ReservaLabs.Api.Services;

/// <summary>Exceção de regra de negócio, convertida em HTTP 422 pelo tratador global.</summary>
public class RegraNegocioException(string mensagem) : Exception(mensagem);

/// <summary>Recurso inexistente, convertido em HTTP 404.</summary>
public class NaoEncontradoException(string mensagem) : Exception(mensagem);

/// <summary>
/// Regras de negócio das reservas. Recebe as dependências pelo construtor
/// (construtor primário do C# 12) e usa <see cref="TimeProvider"/> para que
/// os testes possam controlar o "agora".
/// </summary>
public class ReservaService(
    ILaboratorioRepositorio laboratorios,
    IReservaRepositorio reservas,
    TimeProvider relogio)
{
    public static readonly TimeOnly Abertura = new(7, 0);
    public static readonly TimeOnly Fechamento = new(23, 0);
    public static readonly TimeSpan DuracaoMaxima = TimeSpan.FromHours(4);

    private static readonly object Trava = new();

    public IEnumerable<ReservaResponse> Listar(int? laboratorioId, DateOnly? data) =>
        reservas.Listar()
            .Where(r => laboratorioId is null || r.LaboratorioId == laboratorioId)
            .Where(r => data is null || DateOnly.FromDateTime(r.Inicio) == data)
            .Select(Mapear);

    public ReservaResponse Buscar(int id) =>
        Mapear(reservas.Buscar(id) ?? throw new NaoEncontradoException($"Reserva {id} não encontrada"));

    public ReservaResponse Criar(CriarReservaRequest dados)
    {
        var laboratorio = laboratorios.Buscar(dados.LaboratorioId!.Value)
            ?? throw new NaoEncontradoException($"Laboratório {dados.LaboratorioId} não encontrado");

        var inicio = dados.Inicio!.Value;
        var fim = dados.Fim!.Value;

        ValidarHorario(inicio, fim);

        if (dados.QuantidadeAlunos > laboratorio.Capacidade)
            throw new RegraNegocioException(
                $"{laboratorio.Nome} comporta {laboratorio.Capacidade} alunos; solicitado: {dados.QuantidadeAlunos}");

        // A trava impede que duas requisições simultâneas reservem o mesmo horário
        lock (Trava)
        {
            var conflito = reservas.DoLaboratorio(laboratorio.Id).FirstOrDefault(r => r.ConflitaCom(inicio, fim));
            if (conflito is not null)
                throw new RegraNegocioException(
                    $"Conflito com a reserva {conflito.Id} ({conflito.Disciplina}), " +
                    $"das {conflito.Inicio:HH\\:mm} às {conflito.Fim:HH\\:mm}");

            var reserva = reservas.Adicionar(new Reserva
            {
                LaboratorioId = laboratorio.Id,
                Responsavel = dados.Responsavel.Trim(),
                Disciplina = dados.Disciplina.Trim(),
                Inicio = inicio,
                Fim = fim,
                QuantidadeAlunos = dados.QuantidadeAlunos,
                CriadaEm = relogio.GetLocalNow().DateTime
            });
            return Mapear(reserva);
        }
    }

    public void Cancelar(int id)
    {
        var reserva = reservas.Buscar(id) ?? throw new NaoEncontradoException($"Reserva {id} não encontrada");
        if (reserva.Status == StatusReserva.Cancelada)
            throw new RegraNegocioException($"A reserva {id} já está cancelada");
        if (reserva.Inicio <= relogio.GetLocalNow().DateTime)
            throw new RegraNegocioException("Não é possível cancelar uma reserva que já começou");
        reserva.Cancelar();
    }

    public OcupacaoDiaResponse Ocupacao(int laboratorioId, DateOnly data)
    {
        var laboratorio = laboratorios.Buscar(laboratorioId)
            ?? throw new NaoEncontradoException($"Laboratório {laboratorioId} não encontrado");

        var doDia = reservas.DoLaboratorio(laboratorioId)
            .Where(r => r.Status == StatusReserva.Ativa && DateOnly.FromDateTime(r.Inicio) == data)
            .OrderBy(r => r.Inicio)
            .ToList();

        var horas = doDia.Sum(r => (r.Fim - r.Inicio).TotalHours);
        var horasDisponiveis = (Fechamento - Abertura).TotalHours;

        return new OcupacaoDiaResponse(laboratorio.Id, laboratorio.Nome, data, horas,
            Math.Round(horas / horasDisponiveis * 100, 1), doDia.Select(Mapear).ToList());
    }

    private void ValidarHorario(DateTime inicio, DateTime fim)
    {
        if (fim <= inicio)
            throw new RegraNegocioException("O fim deve ser posterior ao início");
        if (inicio <= relogio.GetLocalNow().DateTime)
            throw new RegraNegocioException("Não é possível reservar no passado");
        if (inicio.Date != fim.Date)
            throw new RegraNegocioException("A reserva deve começar e terminar no mesmo dia");
        if (fim - inicio > DuracaoMaxima)
            throw new RegraNegocioException($"Duração máxima: {DuracaoMaxima.TotalHours} horas");
        if (TimeOnly.FromDateTime(inicio) < Abertura || TimeOnly.FromDateTime(fim) > Fechamento)
            throw new RegraNegocioException($"Os laboratórios funcionam das {Abertura:HH\\:mm} às {Fechamento:HH\\:mm}");
        if (inicio.DayOfWeek == DayOfWeek.Sunday)
            throw new RegraNegocioException("Não há reservas aos domingos");
    }

    private ReservaResponse Mapear(Reserva r) => new(
        r.Id, r.LaboratorioId, laboratorios.Buscar(r.LaboratorioId)?.Nome ?? "?",
        r.Responsavel, r.Disciplina, r.Inicio, r.Fim, r.QuantidadeAlunos, r.Status.ToString());
}
