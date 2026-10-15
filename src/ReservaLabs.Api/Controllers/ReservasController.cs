using Microsoft.AspNetCore.Mvc;
using ReservaLabs.Api.Dtos;
using ReservaLabs.Api.Services;

namespace ReservaLabs.Api.Controllers;

[ApiController]
[Route("api/reservas")]
[Produces("application/json")]
public class ReservasController(ReservaService servico) : ControllerBase
{
    /// <summary>Lista reservas com filtros opcionais por laboratório e data.</summary>
    [HttpGet]
    public IEnumerable<ReservaResponse> Listar([FromQuery] int? laboratorioId, [FromQuery] DateOnly? data) =>
        servico.Listar(laboratorioId, data);

    /// <summary>Busca uma reserva pelo id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ReservaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ReservaResponse Buscar(int id) => servico.Buscar(id);

    /// <summary>Cria uma reserva, validando horário, capacidade e conflitos.</summary>
    [HttpPost]
    [ProducesResponseType<ReservaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ActionResult<ReservaResponse> Criar(CriarReservaRequest dados)
    {
        var criada = servico.Criar(dados);
        return CreatedAtAction(nameof(Buscar), new { id = criada.Id }, criada);   // 201 + Location
    }

    /// <summary>Cancela uma reserva que ainda não começou.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Cancelar(int id)
    {
        servico.Cancelar(id);
        return NoContent();
    }
}
