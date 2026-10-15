namespace ReservaLabs.Tests;

/// <summary>
/// Relógio controlável para os testes: o "agora" é fixo, então os testes
/// não dependem da data em que são executados.
/// </summary>
public class RelogioFixo(DateTimeOffset agora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => agora;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    /// <summary>Segunda-feira, 9 de março de 2026, 08:00.</summary>
    public static RelogioFixo Padrao() => new(new DateTimeOffset(2026, 3, 9, 8, 0, 0, TimeSpan.Zero));
}
