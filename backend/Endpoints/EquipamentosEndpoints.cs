using MySqlConnector;
using TechService.Api.Data;
using TechService.Api.Models;

namespace TechService.Api.Endpoints;

public static class EquipamentosEndpoints
{
    public static void MapEquipamentosEndpoints(this WebApplication app)
    {
        // ==========================================================
        // Listar equipamentos ativos
        // ==========================================================
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

        // ==========================================================
        // Obter equipamento por ID
        // ==========================================================
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
        .WithName("ObterEquipamento")
        .WithSummary("Obter equipamento por ID");

        // ==========================================================
        // Criar equipamento
        // ==========================================================
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
        .WithName("CriarEquipamento")
        .WithSummary("Criar novo equipamento");

        // ==========================================================
        // Atualizar equipamento
        // ==========================================================
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
        .WithName("AtualizarEquipamento")
        .WithSummary("Atualizar equipamento existente");

        // ==========================================================
        // Desativar equipamento (Soft Delete)
        // ==========================================================
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
        .WithName("DesativarEquipamento")
        .WithSummary("Desativar equipamento existente");

        // ==========================================================
        // DESAFIO 3: Equipamentos de um determinado cliente
        // ==========================================================
        app.MapGet("/api/equipamentos/cliente/{id_cliente}", async (int id_cliente, MySqlConnectionFactory factory) =>
        {
            const string sql = "SELECT * FROM equipamentos WHERE id_cliente = @id_cliente AND status = 1;";
            var equipamentos = new List<Equipamento>();

            await using var conn = factory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id_cliente", id_cliente);
            
            await using var reader = await cmd.ExecuteReaderAsync();
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
                    Status = reader.GetInt32("status")
                });
            }
            return Results.Ok(equipamentos);
        })
        .WithName("EquipamentosPorCliente")
        .WithSummary("Listar equipamentos por cliente");
    }
}