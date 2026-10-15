using ReservaLabs.Api.Dtos;
using ReservaLabs.Api.Repositories;
using ReservaLabs.Api.Services;

namespace ReservaLabs.Tests;

/// <summary>Testes de unidade das regras de negócio (sem HTTP).</summary>
public class ReservaServiceTests
{
    private readonly ReservaService _servico = new(
        new LaboratorioRepositorioMemoria(),
        new ReservaRepositorioMemoria(),
        RelogioFixo.Padrao());

    // Terça-feira, 10/03/2026: um dia depois do "agora" dos testes
    private static CriarReservaRequest Pedido(int lab, int horaInicio, int horaFim, int alunos = 20, int dia = 10) =>
        new(lab, "Prof. Eduardo", "Banco de Dados",
            new DateTime(2026, 3, dia, horaInicio, 0, 0), new DateTime(2026, 3, dia, horaFim, 0, 0), alunos);

    [Fact]
    public void Cria_reserva_valida()
    {
        var reserva = _servico.Criar(Pedido(1, 19, 22));

        Assert.Equal(1, reserva.Id);
        Assert.Equal("Ativa", reserva.Status);
        Assert.Equal("Laboratório de Informática 1", reserva.Laboratorio);
    }

    [Theory]
    [InlineData(20, 23)]   // começa durante a reserva existente
    [InlineData(18, 20)]   // termina durante a reserva existente
    [InlineData(19, 22)]   // mesmo horário
    [InlineData(20, 21)]   // contida na existente
    public void Rejeita_horario_sobreposto(int inicio, int fim)
    {
        _servico.Criar(Pedido(1, 19, 22));

        var erro = Assert.Throws<RegraNegocioException>(() => _servico.Criar(Pedido(1, inicio, fim)));
        Assert.Contains("Conflito", erro.Message);
    }

    [Fact]
    public void Permite_horarios_encostados()
    {
        _servico.Criar(Pedido(1, 19, 21));

        var seguinte = _servico.Criar(Pedido(1, 21, 23));   // começa exatamente quando a outra termina

        Assert.Equal(2, seguinte.Id);
    }

    [Fact]
    public void Mesmo_horario_em_laboratorios_diferentes_nao_conflita()
    {
        _servico.Criar(Pedido(1, 19, 22));

        var outra = _servico.Criar(Pedido(2, 19, 22));

        Assert.Equal(2, outra.LaboratorioId);
    }

    [Fact]
    public void Rejeita_turma_acima_da_capacidade()
    {
        var erro = Assert.Throws<RegraNegocioException>(() => _servico.Criar(Pedido(3, 8, 10, alunos: 25)));
        Assert.Contains("comporta 20", erro.Message);
    }

    [Theory]
    [InlineData(6, 8, "funcionam")]        // antes da abertura
    [InlineData(8, 13, "Duração máxima")]  // 5 horas
    [InlineData(10, 9, "posterior")]       // fim antes do início
    public void Rejeita_horarios_invalidos(int inicio, int fim, string trechoDaMensagem)
    {
        var erro = Assert.Throws<RegraNegocioException>(() => _servico.Criar(Pedido(1, inicio, fim)));
        Assert.Contains(trechoDaMensagem, erro.Message);
    }

    [Fact]
    public void Rejeita_domingo()
    {
        Assert.Throws<RegraNegocioException>(() => _servico.Criar(Pedido(1, 8, 10, dia: 15)));   // 15/03/2026 é domingo
    }

    [Fact]
    public void Rejeita_reserva_no_passado()
    {
        Assert.Throws<RegraNegocioException>(() => _servico.Criar(Pedido(1, 8, 10, dia: 6)));
    }

    [Fact]
    public void Reserva_cancelada_libera_o_horario()
    {
        var primeira = _servico.Criar(Pedido(1, 19, 22));
        _servico.Cancelar(primeira.Id);

        var nova = _servico.Criar(Pedido(1, 19, 22));

        Assert.Equal(2, nova.Id);
    }

    [Fact]
    public void Ocupacao_calcula_horas_e_percentual()
    {
        _servico.Criar(Pedido(1, 8, 12));
        _servico.Criar(Pedido(1, 19, 23));

        var ocupacao = _servico.Ocupacao(1, new DateOnly(2026, 3, 10));

        Assert.Equal(8, ocupacao.HorasReservadas);
        Assert.Equal(50.0, ocupacao.TaxaOcupacao);   // 8 de 16 horas de funcionamento
        Assert.Equal(2, ocupacao.Reservas.Count);
    }

    [Fact]
    public void Laboratorio_inexistente_gera_nao_encontrado()
    {
        Assert.Throws<NaoEncontradoException>(() => _servico.Criar(Pedido(99, 8, 10)));
    }
}
