using TechService.Api.Data;
using TechService.Api.Endpoints; 

var builder = WebApplication.CreateBuilder(args);

// Serviços usados pelo Swagger/OpenAPI.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Uma única factory reutilizada para criar ligações ao MySQL.
builder.Services.AddSingleton<MySqlConnectionFactory>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Endpoint mantido da Versão 0.
app.MapGet("/", () => Results.Ok(new
{
    mensagem = "Olá! Bem-vindo à API TechService - Versão 1",
    versao = "V1",
    estado = "API ligada ao MySQL, agora com estrutura Modular!",
}))
.WithName("EstadoDaApi")
.WithSummary("Verificar o estado da API")
.Produces(StatusCodes.Status200OK);

// ==============================================================================
// REGISTO DOS ENDPOINTS MODULARES
// ==============================================================================
// Estas 3 linhas carregam as rotas todas para o Swagger!
app.MapClientesEndpoints();
app.MapEquipamentosEndpoints();
app.MapOrdensServicoEndpoints();

app.Run();