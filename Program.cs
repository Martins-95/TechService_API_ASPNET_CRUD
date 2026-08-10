using MySqlConnector;
using TechService.Api.Data;
using TechService.Api.Models;

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
    estado = "API ligada ao MySQL",
    endpoint_disponivel = "GET /api/clientes"
}))
.WithName("EstadoDaApi")
.WithSummary("Verificar o estado da API")
.Produces(StatusCodes.Status200OK);

// Versão 1: listar clientes ativos da tabela clientes.
app.MapGet("/api/clientes", async (MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT
            id_cliente,
            nome,
            email,
            telefone,
            status,
            created_at,
            updated_at,
            deleted_at
        FROM clientes
        WHERE status = 1
        ORDER BY nome;
        """;

    var clientes = new List<Cliente>();

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    var ordinalIdCliente = reader.GetOrdinal("id_cliente");
    var ordinalNome = reader.GetOrdinal("nome");
    var ordinalEmail = reader.GetOrdinal("email");
    var ordinalTelefone = reader.GetOrdinal("telefone");
    var ordinalStatus = reader.GetOrdinal("status");
    var ordinalCreatedAt = reader.GetOrdinal("created_at");
    var ordinalUpdatedAt = reader.GetOrdinal("updated_at");
    var ordinalDeletedAt = reader.GetOrdinal("deleted_at");

    while (await reader.ReadAsync())
    {
        clientes.Add(new Cliente
        {
            IdCliente = reader.GetInt32(ordinalIdCliente),
            Nome = reader.GetString(ordinalNome),
            Email = reader.GetString(ordinalEmail),
            Telefone = reader.IsDBNull(ordinalTelefone)
                ? null
                : reader.GetString(ordinalTelefone),
            Status = reader.GetInt32(ordinalStatus),
            CreatedAt = reader.GetDateTime(ordinalCreatedAt),
            UpdatedAt = reader.IsDBNull(ordinalUpdatedAt)
                ? null
                : reader.GetDateTime(ordinalUpdatedAt),
            DeletedAt = reader.IsDBNull(ordinalDeletedAt)
                ? null
                : reader.GetDateTime(ordinalDeletedAt)
        });
    }

    return Results.Ok(clientes);
})
.WithName("ListarClientes")
.WithSummary("Listar clientes ativos")
.WithDescription("Devolve os clientes da tabela clientes cujo status é igual a 1.")
.Produces<List<Cliente>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status500InternalServerError);


// Versão 2: Obter um cliente específico pelo ID
app.MapGet("/api/clientes/{id}", async (int id, MySqlConnectionFactory factory) =>
{
    const string sql = @"SELECT id_cliente, nome, email, telefone, status
                         FROM clientes
                         WHERE id_cliente = @id";

    await using var conn = factory.CreateConnection();
    await conn.OpenAsync();

    await using var cmd = new MySqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", id);

    await using var reader = await cmd.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        var cliente = new Cliente
        {
            IdCliente = reader.GetInt32("id_cliente"),
            Nome = reader.GetString("nome"),
            Email = reader.GetString("email"),
            Telefone = reader.GetString("telefone"),
            Status = reader.GetInt32("status")
        };
        return Results.Ok(cliente);
    }

    return Results.NotFound();
})
.WithName("ObterCliente")
.WithSummary("Obter cliente por ID")
.WithDescription("Devolve os dados de um cliente específico pelo seu ID.")
;

// V3 Atualizar os dados de um cliente existente
app.MapPut("/api/clientes/{id}", async (int id, Cliente updatedCliente, MySqlConnectionFactory factory) =>
{
    const string sql = @"UPDATE clientes
                         SET nome = @nome, email = @email, telefone = @telefone
                         WHERE id_cliente = @id";

    await using var conn = factory.CreateConnection();
    await conn.OpenAsync();

    await using var cmd = new MySqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", id);
    cmd.Parameters.AddWithValue("@nome", updatedCliente.Nome);
    cmd.Parameters.AddWithValue("@email", updatedCliente.Email);
    cmd.Parameters.AddWithValue("@telefone", updatedCliente.Telefone ?? (object)DBNull.Value);

    await cmd.ExecuteNonQueryAsync();

    if (cmd.ExecuteNonQuery() == 0)
    {
        return Results.NotFound(new { mensagem = $"Cliente com ID {id} não encontrado." });
    }
    else
    {
        return Results.Ok(new { mensagem = $"Cliente com ID {id} atualizado com sucesso." });
    }
})
.WithName("AtualizarCliente")
.WithSummary("Atualizar cliente existente")
.WithDescription("Atualiza os dados de um cliente existente pelo seu ID. Retorna uma mensagem de sucesso ou erro.")
;     

// V3 Atualizar os dados de um cliente existente
app.MapDelete("/api/clientes/{id_cliente}", async (int id_cliente, MySqlConnectionFactory factory) =>
{
    const string sql = "UPDATE clientes SET status = 0, deleted_at = NOW() WHERE id_cliente = @id_cliente;";

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id_cliente", id_cliente);

    var linhasAfetadas = await command.ExecuteNonQueryAsync();

    if (linhasAfetadas == 0)
    {
        return Results.NotFound(new { mensagem = $"Cliente com ID {id_cliente} não encontrado." });
    }
    else
    {
        return Results.Ok(new { mensagem = $"Cliente com ID {id_cliente} desativado com sucesso." });
    }
})
.WithName("DesativarCliente")
.WithSummary("Desativar cliente existente")
.WithDescription("Desativa um cliente existente pelo seu ID, alterando o status para 0 e registrando a data de exclusão. Retorna uma mensagem de sucesso ou erro.")
;
app.Run();