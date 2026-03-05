using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using DepotService.Models;

namespace DepotService.Data
{
    public class EmpirumRepository
    {
        private readonly string _connectionString;

        public EmpirumRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Testet die Datenbankverbindung
        /// </summary>
        public async Task<(bool Success, string Message)> TestConnectionAsync()
        {
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                return (true, "Verbindung erfolgreich");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database connection test failed: {ex.Message}");
                return (false, $"Verbindung fehlgeschlagen: {ex.Message}");
            }
        }

        /// <summary>
        /// Lädt alle verfügbaren Standorte (Domains)
        /// </summary>
        public async Task<List<string>> GetLocationsAsync()
        {
            var sql = @"
SELECT DISTINCT Domain
FROM dbo.UEMDepotServerStatus
WHERE DepotSyncId IS NOT NULL
  AND Domain IS NOT NULL
  AND Domain <> ''
ORDER BY Domain;";

            var result = new List<string>();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);

            while (await reader.ReadAsync())
            {
                var domain = reader["Domain"] as string;
                if (!string.IsNullOrWhiteSpace(domain))
                {
                    result.Add(domain);
                }
            }

            return result;
        }

        /// <summary>
        /// Lädt alle Depot-Server, die den neuen Sync unterstützen (DepotSyncId IS NOT NULL)
        /// </summary>
        public async Task<List<DepotDto>> GetDepotsAsync(string? location = null)
        {
            var sql = @"
SELECT
    id AS Id,
    Computer,
    Domain,
    LastCheck,
    Status,
    Info,
    CreatedTime,
    DepotSyncId
FROM dbo.UEMDepotServerStatus
WHERE DepotSyncId IS NOT NULL";

            if (!string.IsNullOrWhiteSpace(location))
            {
                sql += " AND Domain = @Domain";
            }

            sql += " ORDER BY Domain, Computer;";

            var result = new List<DepotDto>();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);

            if (!string.IsNullOrWhiteSpace(location))
            {
                cmd.Parameters.Add(new SqlParameter("@Domain", SqlDbType.NVarChar, 255) { Value = location });
            }

            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);

            while (await reader.ReadAsync())
            {
                result.Add(new DepotDto
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Computer = reader["Computer"] as string ?? "",
                    Domain = reader["Domain"] as string ?? "",
                    LastCheck = reader["LastCheck"] != DBNull.Value
                        ? (DateTime?)reader.GetDateTime(reader.GetOrdinal("LastCheck"))
                        : null,
                    Status = reader.GetInt32(reader.GetOrdinal("Status")),
                    Info = reader["Info"] as string,
                    CreatedTime = reader["CreatedTime"] != DBNull.Value
                        ? (DateTime?)reader.GetDateTime(reader.GetOrdinal("CreatedTime"))
                        : null,
                    DepotSyncId = reader["DepotSyncId"] != DBNull.Value
                        ? reader["DepotSyncId"].ToString()
                        : null
                });
            }

            return result;
        }

        /// <summary>
        /// Lädt alle verfügbaren Job-Namen
        /// </summary>
        public async Task<List<string>> GetJobNamesAsync()
        {
            var sql = @"
SELECT DISTINCT JobName
FROM dbo.UEMDepotSyncStatusTable
WHERE JobName IS NOT NULL
  AND JobName <> ''
ORDER BY JobName;";

            var result = new List<string>();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);

            while (await reader.ReadAsync())
            {
                var jobName = reader["JobName"] as string;
                if (!string.IsNullOrWhiteSpace(jobName))
                {
                    result.Add(jobName);
                }
            }

            return result;
        }

        /// <summary>
        /// Erstellt einen neuen StartSync-Job in der Queue
        /// </summary>
        public async Task<int> EnqueueStartSyncAsync(string computer, string domain, string jobName)
        {
            if (string.IsNullOrWhiteSpace(computer))
                throw new ArgumentException("Computer cannot be empty", nameof(computer));
            if (string.IsNullOrWhiteSpace(domain))
                throw new ArgumentException("Domain cannot be empty", nameof(domain));
            if (string.IsNullOrWhiteSpace(jobName))
                throw new ArgumentException("JobName cannot be empty", nameof(jobName));

            var parameters = new
            {
                Computer = computer,
                Domain = domain,
                JobName = jobName
            };

            var parametersJson = JsonSerializer.Serialize(parameters);

            var sql = @"
INSERT INTO dbo.UEMJobs (Command, Status, Parameters, InsertTimeStamp)
VALUES (@Command, 0, @Parameters, GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@Command", SqlDbType.NVarChar, 255) { Value = "StartSync" });
            cmd.Parameters.Add(new SqlParameter("@Parameters", SqlDbType.NVarChar) { Value = parametersJson });

            var jobId = (int)await cmd.ExecuteScalarAsync();
            return jobId;
        }

        /// <summary>
        /// Erstellt mehrere StartSync-Jobs in der Queue (Batch-Operation)
        /// </summary>
        public async Task EnqueueStartSyncForManyAsync(IEnumerable<DepotDto> depots, string jobName)
        {
            if (depots == null || !depots.Any())
                throw new ArgumentException("Depots list cannot be empty", nameof(depots));
            if (string.IsNullOrWhiteSpace(jobName))
                throw new ArgumentException("JobName cannot be empty", nameof(jobName));

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var transaction = conn.BeginTransaction();

            var sql = @"
INSERT INTO dbo.UEMJobs (Command, Status, Parameters, InsertTimeStamp)
VALUES (@Command, 0, @Parameters, GETDATE());";

            try
            {
                await using var cmd = new SqlCommand(sql, conn, transaction);
                cmd.Parameters.Add("@Command", SqlDbType.NVarChar, 255).Value = "StartSync";
                var pParams = cmd.Parameters.Add("@Parameters", SqlDbType.NVarChar);

                foreach (var depot in depots)
                {
                    pParams.Value = JsonSerializer.Serialize(new
                    {
                        Computer = depot.Computer,
                        Domain = depot.Domain,
                        JobName = jobName
                    });

                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Erstellt einen Job in der Queue
        /// </summary>
        public async Task<int> CreateJobAsync(string command, int status, object parameters)
        {
            var parametersJson = JsonSerializer.Serialize(parameters);

            var sql = @"
INSERT INTO dbo.UEMJobs (Command, Status, Parameters, InsertTimeStamp)
VALUES (@Command, @Status, @Parameters, GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@Command", SqlDbType.NVarChar, 255) { Value = command });
            cmd.Parameters.Add(new SqlParameter("@Status", SqlDbType.Int) { Value = status });
            cmd.Parameters.Add(new SqlParameter("@Parameters", SqlDbType.NVarChar) { Value = parametersJson });

            var jobId = (int)await cmd.ExecuteScalarAsync();
            return jobId;
        }
    }
}
