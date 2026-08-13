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

// v4: Criar um novo cliente
app.MapPost("/api/clientes", async (Cliente newCliente, MySqlConnectionFactory factory) =>
{
    const string sql = @"INSERT INTO clientes (nome, email, telefone, status, created_at)
                         VALUES (@nome, @email, @telefone, 1, NOW());
                         SELECT LAST_INSERT_ID();"; 
    
    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@nome", newCliente.Nome);
    command.Parameters.AddWithValue("@email", newCliente.Email);
    command.Parameters.AddWithValue("@telefone", newCliente.Telefone ?? (object)DBNull.Value);

    var newId = Convert.ToInt32(await command.ExecuteScalarAsync());

    return Results.Created($"/api/clientes/{newId}", new { id_cliente = newId, mensagem = "Cliente criado com sucesso." });
})
.WithName("CriarCliente")
.WithSummary("Criar novo cliente")
.WithDescription("Cria um novo cliente na tabela clientes. Retorna o ID do novo cliente e uma mensagem de sucesso.")
.Produces(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status500InternalServerError)
;

// ==============================================================================
// CRUD: EQUIPAMENTOS
// ==============================================================================

// Listar equipamentos ativos
app.MapGet("/api/equipamentos", async (MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT id_equipamento, id_cliente, tipo, marca, modelo, numero_serie, observacoes, status, created_at, updated_at, deleted_at 
        FROM equipamentos 
        WHERE status = 1;
        """;

    var equipamentos = new List<Equipamento>();

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        equipamentos.Add(new Equipamento
        {
            IdEquipamento = reader.GetInt32("id_equipamento"),
            IdCliente = reader.GetInt32("id_cliente"),
            Tipo = reader.IsDBNull(reader.GetOrdinal("tipo")) ? null : reader.GetString("tipo"),
            Marca = reader.IsDBNull(reader.GetOrdinal("marca")) ? null : reader.GetString("marca"),
            Modelo = reader.IsDBNull(reader.GetOrdinal("modelo")) ? null : reader.GetString("modelo"),
            NumeroSerie = reader.IsDBNull(reader.GetOrdinal("numero_serie")) ? null : reader.GetString("numero_serie"),
            Observacoes = reader.IsDBNull(reader.GetOrdinal("observacoes")) ? null : reader.GetString("observacoes"),
            Status = reader.GetInt32("status"),
            CreatedAt = reader.GetDateTime("created_at"),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime("updated_at"),
            DeletedAt = reader.IsDBNull(reader.GetOrdinal("deleted_at")) ? null : reader.GetDateTime("deleted_at")
        });
    }
    return Results.Ok(equipamentos);
})
.WithName("ListarEquipamentos")
.WithSummary("Listar equipamentos ativos");

// Obter equipamento por ID
app.MapGet("/api/equipamentos/{id}", async (int id, MySqlConnectionFactory factory) =>
{
    const string sql = "SELECT * FROM equipamentos WHERE id_equipamento = @id";
    
    await using var conn = factory.CreateConnection();
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", id);
    
    await using var reader = await cmd.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        var equipamento = new Equipamento
        {
            IdEquipamento = reader.GetInt32("id_equipamento"),
            IdCliente = reader.GetInt32("id_cliente"),
            Tipo = reader.IsDBNull(reader.GetOrdinal("tipo")) ? null : reader.GetString("tipo"),
            Marca = reader.IsDBNull(reader.GetOrdinal("marca")) ? null : reader.GetString("marca"),
            Modelo = reader.IsDBNull(reader.GetOrdinal("modelo")) ? null : reader.GetString("modelo"),
            NumeroSerie = reader.IsDBNull(reader.GetOrdinal("numero_serie")) ? null : reader.GetString("numero_serie"),
            Observacoes = reader.IsDBNull(reader.GetOrdinal("observacoes")) ? null : reader.GetString("observacoes"),
            Status = reader.GetInt32("status")
        };
        return Results.Ok(equipamento);
    }
    return Results.NotFound(new { mensagem = $"Equipamento com ID {id} não encontrado." });
})
.WithName("ObterEquipamento");

// Criar equipamento
app.MapPost("/api/equipamentos", async (Equipamento novoEq, MySqlConnectionFactory factory) =>
{
    const string sql = """
        INSERT INTO equipamentos (id_cliente, tipo, marca, modelo, numero_serie, observacoes, status, created_at) 
        VALUES (@id_cliente, @tipo, @marca, @modelo, @numero_serie, @observacoes, 1, NOW());
        SELECT LAST_INSERT_ID();
        """;

    await using var conn = factory.CreateConnection();
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand(sql, conn);
    
    cmd.Parameters.AddWithValue("@id_cliente", novoEq.IdCliente);
    cmd.Parameters.AddWithValue("@tipo", novoEq.Tipo ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("@marca", novoEq.Marca ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("@modelo", novoEq.Modelo ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("@numero_serie", novoEq.NumeroSerie ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("@observacoes", novoEq.Observacoes ?? (object)DBNull.Value);

    var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
    return Results.Created($"/api/equipamentos/{newId}", new { id_equipamento = newId, mensagem = "Equipamento criado com sucesso." });
})
.WithName("CriarEquipamento");

// Atualizar equipamento
app.MapPut("/api/equipamentos/{id}", async (int id, Equipamento eq, MySqlConnectionFactory factory) =>
{
    const string sql = """
        UPDATE equipamentos 
        SET id_cliente = @id_cliente, tipo = @tipo, marca = @marca, 
            modelo = @modelo, numero_serie = @numero_serie, observacoes = @observacoes, updated_at = NOW() 
        WHERE id_equipamento = @id;
        """;

    await using var conn = factory.CreateConnection();
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand(sql, conn);
    
    cmd.Parameters.AddWithValue("@id_cliente", eq.IdCliente);
    cmd.Parameters.AddWithValue("@tipo", eq.Tipo ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("@marca", eq.Marca ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("@modelo", eq.Modelo ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("@numero_serie", eq.NumeroSerie ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("@observacoes", eq.Observacoes ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("@id", id);

    var linhas = await cmd.ExecuteNonQueryAsync();
    if (linhas == 0) return Results.NotFound(new { mensagem = $"Equipamento {id} não encontrado." });
    return Results.Ok(new { mensagem = $"Equipamento {id} atualizado com sucesso." });
})
.WithName("AtualizarEquipamento");

// Desativar equipamento (Soft Delete)
app.MapDelete("/api/equipamentos/{id}", async (int id, MySqlConnectionFactory factory) =>
{
    const string sql = "UPDATE equipamentos SET status = 0, deleted_at = NOW() WHERE id_equipamento = @id;";
    await using var conn = factory.CreateConnection();
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", id);

    var linhas = await cmd.ExecuteNonQueryAsync();
    if (linhas == 0) return Results.NotFound(new { mensagem = $"Equipamento {id} não encontrado." });
    return Results.Ok(new { mensagem = $"Equipamento {id} desativado com sucesso." });
})
.WithName("DesativarEquipamento");


// ==============================================================================
// CRUD: ORDENS DE SERVIÇO
// ==============================================================================

// Listar Ordens ativas
app.MapGet("/api/ordens-servico", async (MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT id_ordem, id_equipamento, defeito_relatado, diagnostico, solucao, status, 
               prioridade, valor_servico, valor_pecas, desconto, valor_total, 
               created_at, updated_at, deleted_at 
        FROM ordens_servico 
        WHERE status > 0;
        """;

    var ordens = new List<OrdemServico>();

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        ordens.Add(new OrdemServico
        {
            IdOrdem = reader.GetInt32("id_ordem"),
            IdEquipamento = reader.GetInt32("id_equipamento"),
            DefeitoRelatado = reader.IsDBNull(reader.GetOrdinal("defeito_relatado")) ? null : reader.GetString("defeito_relatado"),
            Diagnostico = reader.IsDBNull(reader.GetOrdinal("diagnostico")) ? null : reader.GetString("diagnostico"),
            Solucao = reader.IsDBNull(reader.GetOrdinal("solucao")) ? null : reader.GetString("solucao"),
            Status = reader.GetInt32("status"),
            Prioridade = reader.IsDBNull(reader.GetOrdinal("prioridade")) ? 1 : reader.GetInt32("prioridade"),
            ValorServico = reader.IsDBNull(reader.GetOrdinal("valor_servico")) ? 0 : reader.GetDecimal("valor_servico"),
            ValorPecas = reader.IsDBNull(reader.GetOrdinal("valor_pecas")) ? 0 : reader.GetDecimal("valor_pecas"),
            Desconto = reader.IsDBNull(reader.GetOrdinal("desconto")) ? 0 : reader.GetDecimal("desconto"),
            ValorTotal = reader.IsDBNull(reader.GetOrdinal("valor_total")) ? 0 : reader.GetDecimal("valor_total"),
            CreatedAt = reader.GetDateTime("created_at"),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime("updated_at"),
            DeletedAt = reader.IsDBNull(reader.GetOrdinal("deleted_at")) ? null : reader.GetDateTime("deleted_at")
        });
    }
    return Results.Ok(ordens);
})
.WithName("ListarOrdens")
.WithSummary("Listar ordens de serviço ativas");

// Obter Ordem por ID
app.MapGet("/api/ordens-servico/{id}", async (int id, MySqlConnectionFactory factory) =>
{
    const string sql = "SELECT * FROM ordens_servico WHERE id_ordem = @id";
    
    await using var conn = factory.CreateConnection();
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", id);
    
    await using var reader = await cmd.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        var ordem = new OrdemServico
        {
            IdOrdem = reader.GetInt32("id_ordem"),
            IdEquipamento = reader.GetInt32("id_equipamento"),
            DefeitoRelatado = reader.IsDBNull(reader.GetOrdinal("defeito_relatado")) ? null : reader.GetString("defeito_relatado"),
            Diagnostico = reader.IsDBNull(reader.GetOrdinal("diagnostico")) ? null : reader.GetString("diagnostico"),
            Solucao = reader.IsDBNull(reader.GetOrdinal("solucao")) ? null : reader.GetString("solucao"),
            Status = reader.GetInt32("status"),
            Prioridade = reader.IsDBNull(reader.GetOrdinal("prioridade")) ? 1 : reader.GetInt32("prioridade"),
            ValorServico = reader.IsDBNull(reader.GetOrdinal("valor_servico")) ? 0 : reader.GetDecimal("valor_servico"),
            ValorPecas = reader.IsDBNull(reader.GetOrdinal("valor_pecas")) ? 0 : reader.GetDecimal("valor_pecas"),
            Desconto = reader.IsDBNull(reader.GetOrdinal("desconto")) ? 0 : reader.GetDecimal("desconto"),
            ValorTotal = reader.IsDBNull(reader.GetOrdinal("valor_total")) ? 0 : reader.GetDecimal("valor_total")
        };
        return Results.Ok(ordem);
    }
    return Results.NotFound(new { mensagem = $"Ordem com ID {id} não encontrada." });
})
.WithName("ObterOrdem");

// Criar Ordem
app.MapPost("/api/ordens-servico", async (OrdemServico novaOs, MySqlConnectionFactory factory) =>
{
    const string sql = """
        INSERT INTO ordens_servico (id_equipamento, defeito_relatado, diagnostico, solucao, status, 
                                    prioridade, valor_servico, valor_pecas, desconto, valor_total, created_at) 
        VALUES (@id_equipamento, @defeito_relatado, @diagnostico, @solucao, @status, 
                @prioridade, @valor_servico, @valor_pecas, @desconto, @valor_total, NOW());
        SELECT LAST_INSERT_ID();
        """;

    await using var conn = factory.CreateConnection();
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand(sql, conn);
    
    cmd.Parameters.AddWithValue("@id_equipamento", novaOs.IdEquipamento);
    cmd.Parameters.AddWithValue("@defeito_relatado", novaOs.DefeitoRelatado ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("@diagnostico", novaOs.Diagnostico ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("@solucao", novaOs.Solucao ?? (object)DBNull.Value);
    // Definimos o status 1 (Aberto) por padrão, a não ser que enviem outro
    cmd.Parameters.AddWithValue("@status", novaOs.Status > 0 ? novaOs.Status : 1); 
    cmd.Parameters.AddWithValue("@prioridade", novaOs.Prioridade > 0 ? novaOs.Prioridade : 1);
    cmd.Parameters.AddWithValue("@valor_servico", novaOs.ValorServico);
    cmd.Parameters.AddWithValue("@valor_pecas", novaOs.ValorPecas);
    cmd.Parameters.AddWithValue("@desconto", novaOs.Desconto);
    cmd.Parameters.AddWithValue("@valor_total", novaOs.ValorTotal);

    var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
    return Results.Created($"/api/ordens-servico/{newId}", new { id_ordem = newId, mensagem = "Ordem de serviço criada com sucesso." });
})
.WithName("CriarOrdem");

// Atualizar Ordem
app.MapPut("/api/ordens-servico/{id}", async (int id, OrdemServico os, MySqlConnectionFactory factory) =>
{
    const string sql = """
        UPDATE ordens_servico 
        SET id_equipamento = @id_equipamento, defeito_relatado = @defeito_relatado, 
            diagnostico = @diagnostico, solucao = @solucao, status = @status, prioridade = @prioridade,
            valor_servico = @valor_servico, valor_pecas = @valor_pecas, 
            desconto = @desconto, valor_total = @valor_total, updated_at = NOW() 
        WHERE id_ordem = @id;
        """;

    await using var conn = factory.CreateConnection();
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand(sql, conn);
    
    cmd.Parameters.AddWithValue("@id_equipamento", os.IdEquipamento);
    cmd.Parameters.AddWithValue("@defeito_relatado", os.DefeitoRelatado ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("@diagnostico", os.Diagnostico ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("@solucao", os.Solucao ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("@status", os.Status);
    cmd.Parameters.AddWithValue("@prioridade", os.Prioridade);
    cmd.Parameters.AddWithValue("@valor_servico", os.ValorServico);
    cmd.Parameters.AddWithValue("@valor_pecas", os.ValorPecas);
    cmd.Parameters.AddWithValue("@desconto", os.Desconto);
    cmd.Parameters.AddWithValue("@valor_total", os.ValorTotal);
    cmd.Parameters.AddWithValue("@id", id);

    var linhas = await cmd.ExecuteNonQueryAsync();
    if (linhas == 0) return Results.NotFound(new { mensagem = $"Ordem {id} não encontrada." });
    return Results.Ok(new { mensagem = $"Ordem {id} atualizada com sucesso." });
})
.WithName("AtualizarOrdem");

// Desativar Ordem (Soft Delete)
app.MapDelete("/api/ordens-servico/{id}", async (int id, MySqlConnectionFactory factory) =>
{
    // Status 0 significa Ordem Apagada/Cancelada
    const string sql = "UPDATE ordens_servico SET status = 0, deleted_at = NOW() WHERE id_ordem = @id;";
    await using var conn = factory.CreateConnection();
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", id);

    var linhas = await cmd.ExecuteNonQueryAsync();
    if (linhas == 0) return Results.NotFound(new { mensagem = $"Ordem {id} não encontrada." });
    return Results.Ok(new { mensagem = $"Ordem {id} apagada com sucesso." });
})
.WithName("DesativarOrdem");

app.Run();