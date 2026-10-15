namespace ReservaLabs.Api.Models;

/// <summary>Sala ou laboratório que pode ser reservado.</summary>
public class Laboratorio
{
    public int Id { get; set; }
    public required string Nome { get; init; }
    public required string Bloco { get; init; }
    public int Capacidade { get; init; }
    public IReadOnlyList<string> Recursos { get; init; } = [];
}
