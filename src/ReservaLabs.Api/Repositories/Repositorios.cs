using System.Collections.Concurrent;
using ReservaLabs.Api.Models;

namespace ReservaLabs.Api.Repositories;

// Interfaces: o serviço depende da ABSTRAÇÃO, não da implementação (princípio D do SOLID).
// Trocar a memória por um banco (EF Core, Dapper...) não exige mudar o serviço.

public interface ILaboratorioRepositorio
{
    IEnumerable<Laboratorio> Listar();
    Laboratorio? Buscar(int id);
}

public interface IReservaRepositorio
{
    IEnumerable<Reserva> Listar();
    Reserva? Buscar(int id);
    IEnumerable<Reserva> DoLaboratorio(int laboratorioId);
    Reserva Adicionar(Reserva reserva);
}

/// <summary>Repositório em memória com dados iniciais. Registrado como Singleton.</summary>
public class LaboratorioRepositorioMemoria : ILaboratorioRepositorio
{
    private readonly List<Laboratorio> _laboratorios =
    [
        new() { Id = 1, Nome = "Laboratório de Informática 1", Bloco = "A", Capacidade = 40, Recursos = ["Projetor", "40 computadores"] },
        new() { Id = 2, Nome = "Laboratório de Redes",         Bloco = "A", Capacidade = 24, Recursos = ["Racks Cisco", "Projetor"] },
        new() { Id = 3, Nome = "Laboratório de Eletrônica",    Bloco = "B", Capacidade = 20, Recursos = ["Osciloscópios", "Fontes", "Arduino"] },
        new() { Id = 4, Nome = "Ateliê de Arquitetura",        Bloco = "C", Capacidade = 30, Recursos = ["Pranchetas", "Plotter"] }
    ];

    public IEnumerable<Laboratorio> Listar() => _laboratorios;

    public Laboratorio? Buscar(int id) => _laboratorios.FirstOrDefault(l => l.Id == id);
}

/// <summary>Thread-safe: várias requisições podem chegar ao mesmo tempo.</summary>
public class ReservaRepositorioMemoria : IReservaRepositorio
{
    private readonly ConcurrentDictionary<int, Reserva> _reservas = new();
    private int _ultimoId;

    public IEnumerable<Reserva> Listar() => _reservas.Values.OrderBy(r => r.Inicio);

    public Reserva? Buscar(int id) => _reservas.GetValueOrDefault(id);

    public IEnumerable<Reserva> DoLaboratorio(int laboratorioId) =>
        _reservas.Values.Where(r => r.LaboratorioId == laboratorioId);

    public Reserva Adicionar(Reserva reserva)
    {
        reserva.Id = Interlocked.Increment(ref _ultimoId);
        _reservas[reserva.Id] = reserva;
        return reserva;
    }
}
