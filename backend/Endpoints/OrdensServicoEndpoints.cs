using MySqlConnector;
using TechService.Api.Data;
using TechService.Api.Models;

namespace TechService.Api.Endpoints;

public static class OrdensServicoEndpoints
{
    public static void MapOrdensServicoEndpoints(this WebApplication app)
    {
        // ==========================================================
        // Listar Ordens ativas
        // ==========================================================
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

        // ==========================================================
        // Obter Ordem por ID
        // ==========================================================
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
        .WithName("ObterOrdem")
        .WithSummary("Obter ordem de serviço por ID");

        // ==========================================================
        // Criar Ordem
        // ==========================================================
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
            cmd.Parameters.AddWithValue("@status", novaOs.Status > 0 ? novaOs.Status : 1); 
            cmd.Parameters.AddWithValue("@prioridade", novaOs.Prioridade > 0 ? novaOs.Prioridade : 1);
            cmd.Parameters.AddWithValue("@valor_servico", novaOs.ValorServico);
            cmd.Parameters.AddWithValue("@valor_pecas", novaOs.ValorPecas);
            cmd.Parameters.AddWithValue("@desconto", novaOs.Desconto);
            cmd.Parameters.AddWithValue("@valor_total", novaOs.ValorTotal);

            var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return Results.Created($"/api/ordens-servico/{newId}", new { id_ordem = newId, mensagem = "Ordem de serviço criada com sucesso." });
        })
        .WithName("CriarOrdem")
        .WithSummary("Criar nova ordem de serviço");

        // ==========================================================
        // Atualizar Ordem
        // ==========================================================
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
        .WithName("AtualizarOrdem")
        .WithSummary("Atualizar ordem de serviço existente");

        // ==========================================================
        // Desativar Ordem (Soft Delete)
        // ==========================================================
        app.MapDelete("/api/ordens-servico/{id}", async (int id, MySqlConnectionFactory factory) =>
        {
            const string sql = "UPDATE ordens_servico SET status = 0, deleted_at = NOW() WHERE id_ordem = @id;";
            
            await using var conn = factory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var linhas = await cmd.ExecuteNonQueryAsync();
            if (linhas == 0) return Results.NotFound(new { mensagem = $"Ordem {id} não encontrada." });
            return Results.Ok(new { mensagem = $"Ordem {id} apagada com sucesso." });
        })
        .WithName("DesativarOrdem")
        .WithSummary("Desativar ordem de serviço existente");

        // ==========================================================
        // DESAFIO 4: Ordens de Serviço por Cliente (INNER JOIN)
        // ==========================================================
        app.MapGet("/api/ordens-servico/cliente/{id_cliente}", async (int id_cliente, MySqlConnectionFactory factory) =>
        {
            const string sql = """
                SELECT o.* 
                FROM ordens_servico o
                INNER JOIN equipamentos e ON o.id_equipamento = e.id_equipamento
                WHERE e.id_cliente = @id_cliente AND o.status > 0;
                """;

            var ordens = new List<OrdemServico>();

            await using var conn = factory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id_cliente", id_cliente);
            
            await using var reader = await cmd.ExecuteReaderAsync();
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
                    ValorTotal = reader.IsDBNull(reader.GetOrdinal("valor_total")) ? 0 : reader.GetDecimal("valor_total")
                });
            }
            return Results.Ok(ordens);
        })
        .WithName("OrdensPorCliente")
        .WithSummary("Listar ordens de serviço por cliente");

        // ==========================================================
        // DESAFIO 5: Ordens de Serviço por Equipamento
        // ==========================================================
        app.MapGet("/api/ordens-servico/equipamento/{id_equipamento}", async (int id_equipamento, MySqlConnectionFactory factory) =>
        {
            const string sql = "SELECT * FROM ordens_servico WHERE id_equipamento = @id_equipamento AND status > 0;";
            var ordens = new List<OrdemServico>();

            await using var conn = factory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id_equipamento", id_equipamento);
            
            await using var reader = await cmd.ExecuteReaderAsync();
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
                    ValorTotal = reader.IsDBNull(reader.GetOrdinal("valor_total")) ? 0 : reader.GetDecimal("valor_total")
                });
            }
            return Results.Ok(ordens);
        })
        .WithName("OrdensPorEquipamento")
        .WithSummary("Listar ordens de serviço por equipamento");
    }
}