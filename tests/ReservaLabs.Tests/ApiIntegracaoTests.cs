using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ReservaLabs.Api.Dtos;

namespace ReservaLabs.Tests;

/// <summary>
/// Testes de integração: sobem a API em memória (WebApplicationFactory)
/// e fazem requisições HTTP reais contra ela.
/// </summary>
public class ApiIntegracaoTests
{
    private static HttpClient CriarCliente()
    {
        var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(servicos => servicos.AddSingleton<TimeProvider>(RelogioFixo.Padrao())));
        return fabrica.CreateClient();
    }

    private static object NovaReserva(int horaInicio = 19, int horaFim = 22) => new
    {
        laboratorioId = 1,
        responsavel = "Prof. Eduardo",
        disciplina = "Banco de Dados I",
        inicio = new DateTime(2026, 3, 10, horaInicio, 0, 0),
        fim = new DateTime(2026, 3, 10, horaFim, 0, 0),
        quantidadeAlunos = 35
    };

    [Fact]
    public async Task Lista_laboratorios_com_filtro_de_capacidade()
    {
        var cliente = CriarCliente();

        var laboratorios = await cliente.GetFromJsonAsync<List<LaboratorioResponse>>("/api/laboratorios?capacidadeMinima=30");

        Assert.NotNull(laboratorios);
        Assert.All(laboratorios, l => Assert.True(l.Capacidade >= 30));
    }

    [Fact]
    public async Task Post_cria_reserva_e_retorna_201_com_location()
    {
        var cliente = CriarCliente();

        var resposta = await cliente.PostAsJsonAsync("/api/reservas", NovaReserva());

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.EndsWith("/api/reservas/1", resposta.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Conflito_de_horario_retorna_422()
    {
        var cliente = CriarCliente();
        await cliente.PostAsJsonAsync("/api/reservas", NovaReserva(19, 22));

        var resposta = await cliente.PostAsJsonAsync("/api/reservas", NovaReserva(21, 23));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.Contains("Conflito", await resposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Dados_invalidos_retornam_400()
    {
        var cliente = CriarCliente();

        var resposta = await cliente.PostAsJsonAsync("/api/reservas", new { responsavel = "A", quantidadeAlunos = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Reserva_inexistente_retorna_404()
    {
        var cliente = CriarCliente();

        var resposta = await cliente.GetAsync("/api/reservas/999");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Delete_cancela_e_retorna_204()
    {
        var cliente = CriarCliente();
        await cliente.PostAsJsonAsync("/api/reservas", NovaReserva());

        var resposta = await cliente.DeleteAsync("/api/reservas/1");
        var reserva = await cliente.GetFromJsonAsync<ReservaResponse>("/api/reservas/1");

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal("Cancelada", reserva!.Status);
    }
}
