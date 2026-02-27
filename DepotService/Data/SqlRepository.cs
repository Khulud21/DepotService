using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using DepotService.Models;

namespace DepotService.Data
{
    public class SqlRepository
    {
        private readonly string _connectionString;

        public SqlRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<DepotItem>> GetDepotsAsync()
        {
            var sql = @"
SELECT
  d.Computer,
  d.Domain,
  d.Status,
  d.LastCheck,
  -- Ersatz: falls die Zeit-/Sortierspalte in UEMDepotSyncStatusTable anders hei�t, ersetze 'LastSync' unten
  (SELECT TOP 1 s.JobName FROM UEMDepotSyncStatusTable s WHERE s.Computer = d.Computer ORDER BY s.LastSync DESC) AS LastJobName
FROM UEMDepotServerStatus d;
";

            var result = new List<DepotItem>();
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            while (await reader.ReadAsync())
            {
                result.Add(new DepotItem
                {
                    Computer = reader["Computer"] as string ?? "",
                    Domain = reader["Domain"] as string ?? "",
                    Status = reader["Status"]?.ToString() ?? "",
                    LastCheck = reader["LastCheck"] != DBNull.Value ? (DateTime?)reader.GetDateTime(reader.GetOrdinal("LastCheck")) : null,
                    LastJobName = reader["LastJobName"] as string
                });
            }
            return result;
        }

        public async Task CreateJobAsync(string command, int status, object parameters)
        {
            var jsonParams = JsonSerializer.Serialize(parameters);

            var sql = @"
INSERT INTO UEMJobs (Command, Status, Parameters)
VALUES (@Command, @Status, @Parameters);
";
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@Command", SqlDbType.NVarChar) { Value = command });
            cmd.Parameters.Add(new SqlParameter("@Status", SqlDbType.Int) { Value = status });
            cmd.Parameters.Add(new SqlParameter("@Parameters", SqlDbType.NVarChar) { Value = jsonParams });
            await cmd.ExecuteNonQueryAsync();
        }
    }
}