using Microsoft.AspNetCore.Mvc;
using ReservaLabs.Api.Dtos;
using ReservaLabs.Api.Repositories;
using ReservaLabs.Api.Services;

namespace ReservaLabs.Api.Controllers;

[ApiController]
[Route("api/laboratorios")]
[Produces("application/json")]
public class LaboratoriosController(ILaboratorioRepositorio repositorio, ReservaService servico) : ControllerBase
{
    /// <summary>Lista os laboratórios, com filtro opcional por capacidade mínima.</summary>
    [HttpGet]
    public IEnumerable<LaboratorioResponse> Listar([FromQuery] int? capacidadeMinima) =>
        repositorio.Listar()
            .Where(l => capacidadeMinima is null || l.Capacidade >= capacidadeMinima)
            .Select(LaboratorioResponse.De);

    /// <summary>Busca um laboratório pelo id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<LaboratorioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<LaboratorioResponse> Buscar(int id)
    {
        var laboratorio = repositorio.Buscar(id);
        return laboratorio is null ? NotFound() : LaboratorioResponse.De(laboratorio);
    }

    /// <summary>Ocupação do laboratório em uma data (formato AAAA-MM-DD).</summary>
    [HttpGet("{id:int}/ocupacao")]
    public OcupacaoDiaResponse Ocupacao(int id, [FromQuery] DateOnly data) => servico.Ocupacao(id, data);
}
