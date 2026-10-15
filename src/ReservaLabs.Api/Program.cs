using System.Text.Json.Serialization;
using ReservaLabs.Api.Infra;
using ReservaLabs.Api.Repositories;
using ReservaLabs.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Injeção de dependência ----------
// Singleton: uma instância para toda a aplicação (os dados ficam em memória)
builder.Services.AddSingleton<ILaboratorioRepositorio, LaboratorioRepositorioMemoria>();
builder.Services.AddSingleton<IReservaRepositorio, ReservaRepositorioMemoria>();
builder.Services.AddSingleton(TimeProvider.System);
// Scoped: uma instância por requisição HTTP
builder.Services.AddScoped<ReservaService>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratadorDeExcecoes>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opcoes =>
{
    opcoes.SwaggerDoc("v1", new() { Title = "ReservaLabs API", Version = "v1",
        Description = "Reserva de laboratórios e salas de aula da faculdade" });
    var xml = Path.Combine(AppContext.BaseDirectory, "ReservaLabs.Api.xml");
    if (File.Exists(xml))
        opcoes.IncludeXmlComments(xml);   // usa os comentários /// como descrição dos endpoints
});

var app = builder.Build();

// ---------- Pipeline de middlewares (a ordem importa) ----------
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

// Torna a classe Program visível para os testes de integração (WebApplicationFactory)
public partial class Program;
