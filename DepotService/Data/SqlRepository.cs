using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using DepotService.Models;
using Microsoft.Extensions.Logging;

namespace DepotService.Data
{
    public class SqlRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlRepository>? _logger;

        public SqlRepository(string connectionString, ILogger<SqlRepository>? logger = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger;
        }

        #region Connection Management

        /// <summary>
        /// Testet die Verbindung zur Datenbank
        /// </summary>
        public async Task<(bool Success, string Message)> TestConnectionAsync()
        {
            try
            {
                _logger?.LogInformation("Testing database connection...");

                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                _logger?.LogInformation("Database connection successful");
                return (true, "Verbindung erfolgreich");
            }
            catch (SqlException ex)
            {
                _logger?.LogError(ex, "Database connection failed");
                return (false, $"Verbindungsfehler: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error during connection test");
                return (false, $"Unerwarteter Fehler: {ex.Message}");
            }
        }

        /// <summary>
        /// Prüft ob die Verbindung aktiv ist
        /// </summary>
        public async Task<bool> IsConnectionAliveAsync()
        {
            var result = await TestConnectionAsync();
            return result.Success;
        }

        #endregion

        #region Depot Data Retrieval

        /// <summary>
        /// Lädt alle Depot-Einträge ohne Filter
        /// </summary>
        public async Task<List<DepotItem>> GetDepotsAsync()
        {
            return await GetDepotsAsync(null);
        }

        /// <summary>
        /// Lädt Depot-Einträge mit optionalem Standort-Filter
        /// </summary>
        /// <param name="locationFilter">Standort-Filter (Domain), null für alle</param>
        public async Task<List<DepotItem>> GetDepotsAsync(string? locationFilter)
        {
            return await GetDepotsAsync(locationFilter, false);
        }

        /// <summary>
        /// Lädt Depot-Einträge mit optionalem Standort-Filter und Computer-Filter
        /// </summary>
        /// <param name="locationFilter">Standort-Filter (Domain), null für alle</param>
        /// <param name="onlyDepotComputers">True = nur Computer die mit 'DEPOT' beginnen, False = alle</param>
        public async Task<List<DepotItem>> GetDepotsAsync(string? locationFilter, bool onlyDepotComputers)
        {
            try
            {
                _logger?.LogInformation("Fetching depots with location filter: {Filter}, depot-only: {DepotOnly}",
                    locationFilter ?? "None", onlyDepotComputers);

                var sql = @"
SELECT
    Computer,
    Domain,
    LastCheck,
    Status,
    Info
FROM dbo.UEMDepotServerStatus";

                var whereClauses = new List<string>();

                if (onlyDepotComputers)
                {
                    whereClauses.Add("Computer LIKE 'DEPOT%'");
                }

                if (!string.IsNullOrWhiteSpace(locationFilter))
                {
                    whereClauses.Add("Domain = @LocationFilter");
                }

                if (whereClauses.Any())
                {
                    sql += " WHERE " + string.Join(" AND ", whereClauses);
                }

                sql += " ORDER BY Computer;";

                var result = new List<DepotItem>();
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(locationFilter))
                {
                    cmd.Parameters.Add(new SqlParameter("@LocationFilter", SqlDbType.NVarChar, 255) { Value = locationFilter });
                }

                await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
                while (await reader.ReadAsync())
                {
                    result.Add(new DepotItem
                    {
                        Computer = reader["Computer"] as string ?? "",
                        Domain = reader["Domain"] as string ?? "",
                        Status = reader["Status"]?.ToString() ?? "",
                        LastCheck = reader["LastCheck"] != DBNull.Value
                            ? (DateTime?)reader.GetDateTime(reader.GetOrdinal("LastCheck"))
                            : null,
                        Info = reader["Info"] as string
                    });
                }

                _logger?.LogInformation("Fetched {Count} depot items", result.Count);
                return result;
            }
            catch (SqlException ex)
            {
                _logger?.LogError(ex, "SQL error while fetching depots");
                throw new InvalidOperationException($"Fehler beim Laden der Depot-Daten: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error while fetching depots");
                throw;
            }
        }

        /// <summary>
        /// Lädt alle verfügbaren Standorte (Domains)
        /// </summary>
        public async Task<List<string>> GetLocationsAsync()
        {
            try
            {
                _logger?.LogInformation("Fetching all locations");

                var sql = @"
SELECT DISTINCT Domain
FROM UEMDepotServerStatus
WHERE Domain IS NOT NULL AND Domain != ''
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

                _logger?.LogInformation("Found {Count} locations", result.Count);
                return result;
            }
            catch (SqlException ex)
            {
                _logger?.LogError(ex, "SQL error while fetching locations");
                throw new InvalidOperationException($"Fehler beim Laden der Standorte: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error while fetching locations");
                throw;
            }
        }

        /// <summary>
        /// Lädt alle verfügbaren Job-Namen aus der Sync-Status-Tabelle
        /// </summary>
        public async Task<List<string>> GetAvailableJobNamesAsync()
        {
            try
            {
                _logger?.LogInformation("Fetching available job names");

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

                _logger?.LogInformation("Found {Count} job names", result.Count);
                return result;
            }
            catch (SqlException ex)
            {
                _logger?.LogError(ex, "SQL error while fetching job names");
                throw new InvalidOperationException($"Fehler beim Laden der Job-Namen: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error while fetching job names");
                throw;
            }
        }

        /// <summary>
        /// Lädt Depots nach spezifischen Computer-Namen
        /// </summary>
        public async Task<List<DepotItem>> GetDepotsByComputersAsync(IEnumerable<string> computerNames)
        {
            try
            {
                var names = computerNames?.ToList() ?? new List<string>();
                if (!names.Any())
                {
                    return new List<DepotItem>();
                }

                _logger?.LogInformation("Fetching depots for {Count} computers", names.Count);

                var sql = @"
SELECT
    Computer,
    Domain,
    LastCheck,
    Status,
    Info
FROM dbo.UEMDepotServerStatus
WHERE Computer IN (" + string.Join(",", names.Select((_, i) => $"@Computer{i}")) + ");";

                var result = new List<DepotItem>();
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand(sql, conn);
                for (int i = 0; i < names.Count; i++)
                {
                    cmd.Parameters.Add(new SqlParameter($"@Computer{i}", SqlDbType.NVarChar, 255) { Value = names[i] });
                }

                await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
                while (await reader.ReadAsync())
                {
                    result.Add(new DepotItem
                    {
                        Computer = reader["Computer"] as string ?? "",
                        Domain = reader["Domain"] as string ?? "",
                        Status = reader["Status"]?.ToString() ?? "",
                        LastCheck = reader["LastCheck"] != DBNull.Value
                            ? (DateTime?)reader.GetDateTime(reader.GetOrdinal("LastCheck"))
                            : null,
                        Info = reader["Info"] as string
                    });
                }

                return result;
            }
            catch (SqlException ex)
            {
                _logger?.LogError(ex, "SQL error while fetching depots by computers");
                throw new InvalidOperationException($"Fehler beim Laden der Depot-Daten: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error while fetching depots by computers");
                throw;
            }
        }

        #endregion

        #region Job Management

        /// <summary>
        /// Erstellt einen neuen Job in der Datenbank
        /// </summary>
        public async Task<int> CreateJobAsync(string command, int status, object parameters)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(command))
                    throw new ArgumentException("Command cannot be empty", nameof(command));

                _logger?.LogInformation("Creating job: {Command}", command);

                var jsonParams = JsonSerializer.Serialize(parameters);

                var sql = @"
INSERT INTO dbo.UEMJobs (Command, Status, Parameters, InsertTimeStamp)
VALUES (@Command, @Status, @Parameters, GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);";

                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add(new SqlParameter("@Command", SqlDbType.NVarChar, 255) { Value = command });
                cmd.Parameters.Add(new SqlParameter("@Status", SqlDbType.Int) { Value = status });
                cmd.Parameters.Add(new SqlParameter("@Parameters", SqlDbType.NVarChar) { Value = jsonParams });

                var jobId = (int)await cmd.ExecuteScalarAsync();

                _logger?.LogInformation("Job created with ID: {JobId}", jobId);
                return jobId;
            }
            catch (SqlException ex)
            {
                _logger?.LogError(ex, "SQL error while creating job");
                throw new InvalidOperationException($"Fehler beim Erstellen des Jobs: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error while creating job");
                throw;
            }
        }

        /// <summary>
        /// Lädt alle aktiven Jobs
        /// </summary>
        public async Task<List<JobInfo>> GetActiveJobsAsync()
        {
            try
            {
                _logger?.LogInformation("Fetching active jobs");

                var sql = @"
SELECT
  JobID,
  Command,
  Status,
  Parameters,
  CreatedAt,
  StartedAt,
  CompletedAt,
  ErrorMessage
FROM UEMJobs
WHERE Status IN (0, 1)
ORDER BY CreatedAt DESC;";

                var result = new List<JobInfo>();
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);

                while (await reader.ReadAsync())
                {
                    result.Add(ReadJobInfo(reader));
                }

                _logger?.LogInformation("Found {Count} active jobs", result.Count);
                return result;
            }
            catch (SqlException ex)
            {
                _logger?.LogError(ex, "SQL error while fetching active jobs");
                throw new InvalidOperationException($"Fehler beim Laden der Jobs: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error while fetching active jobs");
                throw;
            }
        }

        /// <summary>
        /// Lädt Job-Status nach ID
        /// </summary>
        public async Task<JobInfo?> GetJobStatusAsync(int jobId)
        {
            try
            {
                _logger?.LogInformation("Fetching job status for ID: {JobId}", jobId);

                var sql = @"
SELECT
  JobID,
  Command,
  Status,
  Parameters,
  CreatedAt,
  StartedAt,
  CompletedAt,
  ErrorMessage
FROM UEMJobs
WHERE JobID = @JobId;";

                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add(new SqlParameter("@JobId", SqlDbType.Int) { Value = jobId });

                await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);

                if (await reader.ReadAsync())
                {
                    return ReadJobInfo(reader);
                }

                _logger?.LogWarning("Job not found: {JobId}", jobId);
                return null;
            }
            catch (SqlException ex)
            {
                _logger?.LogError(ex, "SQL error while fetching job status");
                throw new InvalidOperationException($"Fehler beim Laden des Job-Status: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error while fetching job status");
                throw;
            }
        }

        /// <summary>
        /// Aktualisiert den Job-Status
        /// </summary>
        public async Task UpdateJobStatusAsync(int jobId, int status, string? errorMessage = null)
        {
            try
            {
                _logger?.LogInformation("Updating job {JobId} to status {Status}", jobId, status);

                var sql = @"
UPDATE UEMJobs
SET Status = @Status,
    ErrorMessage = @ErrorMessage,
    StartedAt = CASE WHEN @Status = 1 AND StartedAt IS NULL THEN GETDATE() ELSE StartedAt END,
    CompletedAt = CASE WHEN @Status IN (2, 3) THEN GETDATE() ELSE CompletedAt END
WHERE JobID = @JobId;";

                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add(new SqlParameter("@JobId", SqlDbType.Int) { Value = jobId });
                cmd.Parameters.Add(new SqlParameter("@Status", SqlDbType.Int) { Value = status });
                cmd.Parameters.Add(new SqlParameter("@ErrorMessage", SqlDbType.NVarChar)
                {
                    Value = (object?)errorMessage ?? DBNull.Value
                });

                await cmd.ExecuteNonQueryAsync();

                _logger?.LogInformation("Job {JobId} status updated", jobId);
            }
            catch (SqlException ex)
            {
                _logger?.LogError(ex, "SQL error while updating job status");
                throw new InvalidOperationException($"Fehler beim Aktualisieren des Job-Status: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error while updating job status");
                throw;
            }
        }

        private JobInfo ReadJobInfo(SqlDataReader reader)
        {
            return new JobInfo
            {
                JobId = reader.GetInt32(reader.GetOrdinal("JobID")),
                Command = reader["Command"] as string ?? "",
                Status = reader.GetInt32(reader.GetOrdinal("Status")),
                Parameters = reader["Parameters"] as string ?? "",
                CreatedAt = reader["CreatedAt"] != DBNull.Value
                    ? reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                    : DateTime.MinValue,
                StartedAt = reader["StartedAt"] != DBNull.Value
                    ? (DateTime?)reader.GetDateTime(reader.GetOrdinal("StartedAt"))
                    : null,
                CompletedAt = reader["CompletedAt"] != DBNull.Value
                    ? (DateTime?)reader.GetDateTime(reader.GetOrdinal("CompletedAt"))
                    : null,
                ErrorMessage = reader["ErrorMessage"] as string
            };
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Lädt Statistiken über Depot-Status
        /// </summary>
        public async Task<DepotStatistics> GetDepotStatisticsAsync(string? locationFilter = null)
        {
            try
            {
                _logger?.LogInformation("Fetching depot statistics for location: {Filter}", locationFilter ?? "All");

                var sql = @"
SELECT
  COUNT(*) as TotalDepots,
  SUM(CASE WHEN Status = 'Online' THEN 1 ELSE 0 END) as OnlineCount,
  SUM(CASE WHEN Status = 'Offline' THEN 1 ELSE 0 END) as OfflineCount,
  SUM(CASE WHEN Status = 'Warning' THEN 1 ELSE 0 END) as WarningCount,
  SUM(CASE WHEN LastCheck < DATEADD(HOUR, -24, GETDATE()) THEN 1 ELSE 0 END) as OutdatedCount
FROM UEMDepotServerStatus";

                if (!string.IsNullOrWhiteSpace(locationFilter))
                {
                    sql += " WHERE Domain = @LocationFilter";
                }

                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(locationFilter))
                {
                    cmd.Parameters.Add(new SqlParameter("@LocationFilter", SqlDbType.NVarChar, 255) { Value = locationFilter });
                }

                await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);

                if (await reader.ReadAsync())
                {
                    return new DepotStatistics
                    {
                        TotalDepots = reader.GetInt32(0),
                        OnlineCount = reader.GetInt32(1),
                        OfflineCount = reader.GetInt32(2),
                        WarningCount = reader.GetInt32(3),
                        OutdatedCount = reader.GetInt32(4)
                    };
                }

                return new DepotStatistics();
            }
            catch (SqlException ex)
            {
                _logger?.LogError(ex, "SQL error while fetching statistics");
                throw new InvalidOperationException($"Fehler beim Laden der Statistiken: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error while fetching statistics");
                throw;
            }
        }

        #endregion
    }
}