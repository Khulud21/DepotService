using System;
using System.Collections.Generic;
using System.Data;
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
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Lädt alle Depot-Server mit Status
        /// </summary>
        public async Task<List<DepotDto>> GetDepotsAsync()
        {
            var sql = @"
SELECT
    Computer,
    Domain,
    LastCheck,
    Status,
    Info
FROM dbo.UEMDepotServerStatus
ORDER BY Domain, Computer;";

            var result = new List<DepotDto>();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);

            while (await reader.ReadAsync())
            {
                result.Add(new DepotDto
                {
                    Computer = reader["Computer"] as string ?? "",
                    Domain = reader["Domain"] as string ?? "",
                    LastCheck = reader["LastCheck"] != DBNull.Value
                        ? (DateTime?)reader.GetDateTime(reader.GetOrdinal("LastCheck"))
                        : null,
                    Status = reader["Status"] as string ?? "",
                    Info = reader["Info"] as string
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
VALUES (@Command, @Status, @Parameters, GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@Command", SqlDbType.NVarChar, 255) { Value = "StartSync" });
            cmd.Parameters.Add(new SqlParameter("@Status", SqlDbType.Int) { Value = 0 });
            cmd.Parameters.Add(new SqlParameter("@Parameters", SqlDbType.NVarChar) { Value = parametersJson });

            var jobId = (int)await cmd.ExecuteScalarAsync();
            return jobId;
        }
    }
}
