using Npgsql;

namespace WebServer.Services
{
    public interface IDatabaseMigrator
    {
        Task MigrateAsync();
    }

    public class DatabaseMigrator : IDatabaseMigrator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseMigrator> _logger;
        private readonly IConfiguration _configuration;

        // Current schema version - increment when adding new migrations
        private const int CURRENT_SCHEMA_VERSION = 1;

        public DatabaseMigrator(IServiceProvider serviceProvider, ILogger<DatabaseMigrator> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task MigrateAsync()
        {
            _logger.LogInformation("Starting database migrations (schema version {Version})...", CURRENT_SCHEMA_VERSION);

            try
            {
                // Migrate Actions database
                var actionsConnStr = _configuration.GetConnectionString("DbActions");
                if (!string.IsNullOrEmpty(actionsConnStr))
                {
                    await MigrateActionsDbAsync(actionsConnStr);
                }

                // Migrate Device database
                var deviceConnStr = _configuration.GetConnectionString("DbDevice");
                if (!string.IsNullOrEmpty(deviceConnStr))
                {
                    await MigrateDeviceDbAsync(deviceConnStr);
                }

                // Migrate AppAccounts database (Identity)
                var accountsConnStr = _configuration.GetConnectionString("DbAppAccounts");
                if (!string.IsNullOrEmpty(accountsConnStr))
                {
                    await MigrateAppAccountsDbAsync(accountsConnStr);
                }

                _logger.LogInformation("Database migrations completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database migration failed");
            }
        }

        #region Actions Database Migrations

        private async Task MigrateActionsDbAsync(string connectionString)
        {
            _logger.LogInformation("Migrating Actions database...");

            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                // Ensure schema_version table exists
                await EnsureSchemaVersionTableAsync(connection);

                // v1: Ensure all Actions table columns exist
                await MigrateActions_v1_BaseColumnsAsync(connection);

                _logger.LogInformation("Actions database migration completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate Actions database");
            }
        }

        private async Task MigrateActions_v1_BaseColumnsAsync(NpgsqlConnection connection)
        {
            const string migrationName = "Actions_v1_BaseColumns";

            if (await IsMigrationAppliedAsync(connection, migrationName))
                return;

            _logger.LogInformation("Applying migration: {Migration}", migrationName);

            // Actions table columns (ActionTime is the primary key, should already exist)
            await AddColumnIfNotExistsAsync(connection, "Actions", "ActionCode", "integer", "0");
            await AddColumnIfNotExistsAsync(connection, "Actions", "UserId", "text", "''");
            await AddColumnIfNotExistsAsync(connection, "Actions", "ActionServerId", "bigint", "0");
            await AddColumnIfNotExistsAsync(connection, "Actions", "ActionStationId", "bigint", "0");
            await AddColumnIfNotExistsAsync(connection, "Actions", "ActionPowerBankId", "bigint", "0");
            await AddColumnIfNotExistsAsync(connection, "Actions", "ActionPowerBankSlot", "integer", "0");
            await AddColumnIfNotExistsAsync(connection, "Actions", "ActionText", "text", "''");
            await AddColumnIfNotExistsAsync(connection, "Actions", "PaymentAmount", "real", "0");
            await AddColumnIfNotExistsAsync(connection, "Actions", "PaymentInfo", "text", "''");

            await RecordMigrationAsync(connection, migrationName);
        }

        #endregion

        #region Device Database Migrations

        private async Task MigrateDeviceDbAsync(string connectionString)
        {
            _logger.LogInformation("Migrating Device database...");

            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                // Ensure schema_version table exists
                await EnsureSchemaVersionTableAsync(connection);

                // v1: Ensure all Device table columns exist
                await MigrateDevice_v1_BaseColumnsAsync(connection);

                // v1: Ensure all PowerBank table columns exist
                await MigratePowerBank_v1_BaseColumnsAsync(connection);

                // v1: Ensure all Server table columns exist
                await MigrateServer_v1_BaseColumnsAsync(connection);

                _logger.LogInformation("Device database migration completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate Device database");
            }
        }

        private async Task MigrateDevice_v1_BaseColumnsAsync(NpgsqlConnection connection)
        {
            const string migrationName = "Device_v1_BaseColumns";

            if (await IsMigrationAppliedAsync(connection, migrationName))
                return;

            _logger.LogInformation("Applying migration: {Migration}", migrationName);

            // Device table columns
            await AddColumnIfNotExistsAsync(connection, "Device", "DeviceName", "text", "''");
            await AddColumnIfNotExistsAsync(connection, "Device", "HostDeviceId", "bigint", "0");
            await AddColumnIfNotExistsAsync(connection, "Device", "Online", "boolean", "false");
            await AddColumnIfNotExistsAsync(connection, "Device", "Activated", "boolean", "false");
            await AddColumnIfNotExistsAsync(connection, "Device", "CanRegister", "boolean", "false");
            await AddColumnIfNotExistsAsync(connection, "Device", "Registered", "boolean", "false");
            await AddColumnIfNotExistsAsync(connection, "Device", "Slots", "integer", "0");
            await AddColumnIfNotExistsAsync(connection, "Device", "TypeOfUse", "integer", "0");
            await AddColumnIfNotExistsAsync(connection, "Device", "IP", "text", "''");
            await AddColumnIfNotExistsAsync(connection, "Device", "DevMainServer", "text", "''");
            await AddColumnIfNotExistsAsync(connection, "Device", "DevResServer", "text", "''");
            await AddColumnIfNotExistsAsync(connection, "Device", "Owners", "text", "''");
            await AddColumnIfNotExistsAsync(connection, "Device", "SimId", "text", "''");
            await AddColumnIfNotExistsAsync(connection, "Device", "Error", "text", "''");
            await AddColumnIfNotExistsAsync(connection, "Device", "Description", "text", "''");
            await AddColumnIfNotExistsAsync(connection, "Device", "ActivateTime", "timestamp without time zone", "'1970-01-01'");
            await AddColumnIfNotExistsAsync(connection, "Device", "LastOnlineTime", "timestamp without time zone", "'1970-01-01'");
            await AddColumnIfNotExistsAsync(connection, "Device", "FirstOnlineTime", "timestamp without time zone", "'1970-01-01'");
            await AddColumnIfNotExistsAsync(connection, "Device", "LastUpdate", "timestamp without time zone", "'1970-01-01'");
            await AddColumnIfNotExistsAsync(connection, "Device", "UpdateInt", "boolean", "false");
            await AddColumnIfNotExistsAsync(connection, "Device", "UpdateExt", "boolean", "false");

            await RecordMigrationAsync(connection, migrationName);
        }

        private async Task MigratePowerBank_v1_BaseColumnsAsync(NpgsqlConnection connection)
        {
            const string migrationName = "PowerBank_v1_BaseColumns";

            if (await IsMigrationAppliedAsync(connection, migrationName))
                return;

            _logger.LogInformation("Applying migration: {Migration}", migrationName);

            // PowerBank table columns
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "HostDeviceName", "text", "''");
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "HostSlot", "integer", "0");
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "Locked", "boolean", "false");
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "Plugged", "boolean", "false");
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "Charging", "boolean", "false");
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "IsOk", "boolean", "true");
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "Restricted", "boolean", "false");
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "ChargeLevel", "integer", "0");
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "LastGetTime", "timestamp without time zone", "'1970-01-01'");
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "LastPutTime", "timestamp without time zone", "'1970-01-01'");
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "ClientTime", "timestamp with time zone", "'1970-01-01'");
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "Price", "real", "12");
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "Cost", "real", "0");
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "LastUpdate", "timestamp without time zone", "'1970-01-01'");
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "Taken", "boolean", "false");
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "UserId", "text", "''");
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "UpdateInt", "boolean", "false");
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "UpdateExt", "boolean", "false");
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "Reserved", "boolean", "false");
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "ReserveTime", "timestamp without time zone", "NULL");
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "PaymentInfo", "text", "''");
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "SessionId", "text", "''");

            await RecordMigrationAsync(connection, migrationName);
        }

        private async Task MigrateServer_v1_BaseColumnsAsync(NpgsqlConnection connection)
        {
            const string migrationName = "Server_v1_BaseColumns";

            if (await IsMigrationAppliedAsync(connection, migrationName))
                return;

            _logger.LogInformation("Applying migration: {Migration}", migrationName);

            // Server table columns
            await AddColumnIfNotExistsAsync(connection, "Server", "Host", "text", "''");
            await AddColumnIfNotExistsAsync(connection, "Server", "Port", "integer", "0");
            await AddColumnIfNotExistsAsync(connection, "Server", "Login", "text", "''");
            await AddColumnIfNotExistsAsync(connection, "Server", "Password", "text", "''");
            await AddColumnIfNotExistsAsync(connection, "Server", "ReconnectTime", "integer", "30");
            await AddColumnIfNotExistsAsync(connection, "Server", "Connected", "boolean", "false");
            await AddColumnIfNotExistsAsync(connection, "Server", "Error", "text", "''");
            await AddColumnIfNotExistsAsync(connection, "Server", "ConnectTime", "timestamp without time zone", "'1970-01-01'");
            await AddColumnIfNotExistsAsync(connection, "Server", "DisconnectTime", "timestamp without time zone", "'1970-01-01'");
            await AddColumnIfNotExistsAsync(connection, "Server", "LastUpdate", "timestamp without time zone", "'1970-01-01'");
            await AddColumnIfNotExistsAsync(connection, "Server", "DevicesCount", "integer", "0");
            await AddColumnIfNotExistsAsync(connection, "Server", "CertCA", "text", "''");
            await AddColumnIfNotExistsAsync(connection, "Server", "CertCli", "text", "''");
            await AddColumnIfNotExistsAsync(connection, "Server", "CertPass", "text", "''");

            await RecordMigrationAsync(connection, migrationName);
        }

        #endregion

        #region AppAccounts Database Migrations

        private async Task MigrateAppAccountsDbAsync(string connectionString)
        {
            _logger.LogInformation("Migrating AppAccounts database...");

            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                // Ensure schema_version table exists
                await EnsureSchemaVersionTableAsync(connection);

                // Identity tables are managed by ASP.NET Identity,
                // but we can add custom columns here if needed
                // await MigrateAppUser_v1_CustomFieldsAsync(connection);

                _logger.LogInformation("AppAccounts database migration completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate AppAccounts database");
            }
        }

        #endregion

        #region Helper Methods

        private async Task EnsureSchemaVersionTableAsync(NpgsqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS ""__schema_migrations"" (
                    ""MigrationName"" text PRIMARY KEY,
                    ""AppliedAt"" timestamp without time zone DEFAULT now()
                )";

            await using var cmd = new NpgsqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<bool> IsMigrationAppliedAsync(NpgsqlConnection connection, string migrationName)
        {
            var sql = @"SELECT COUNT(*) FROM ""__schema_migrations"" WHERE ""MigrationName"" = @name";

            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("name", migrationName);

            var count = (long)(await cmd.ExecuteScalarAsync() ?? 0);
            return count > 0;
        }

        private async Task RecordMigrationAsync(NpgsqlConnection connection, string migrationName)
        {
            var sql = @"INSERT INTO ""__schema_migrations"" (""MigrationName"") VALUES (@name) ON CONFLICT DO NOTHING";

            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("name", migrationName);
            await cmd.ExecuteNonQueryAsync();

            _logger.LogInformation("Migration recorded: {Migration}", migrationName);
        }

        private async Task AddColumnIfNotExistsAsync(NpgsqlConnection connection, string tableName, string columnName, string columnType, string defaultValue)
        {
            // Check if column exists (case-insensitive for table name)
            var checkSql = @"
                SELECT COUNT(*)
                FROM information_schema.columns
                WHERE table_name ILIKE @tableName
                AND column_name = @columnName";

            await using var checkCmd = new NpgsqlCommand(checkSql, connection);
            checkCmd.Parameters.AddWithValue("tableName", tableName);
            checkCmd.Parameters.AddWithValue("columnName", columnName);

            var exists = (long)(await checkCmd.ExecuteScalarAsync() ?? 0) > 0;

            if (!exists)
            {
                // Find actual table name (case-sensitive)
                var findTableSql = @"SELECT table_name FROM information_schema.tables WHERE table_name ILIKE @tableName LIMIT 1";
                await using var findCmd = new NpgsqlCommand(findTableSql, connection);
                findCmd.Parameters.AddWithValue("tableName", tableName);
                var actualTableName = await findCmd.ExecuteScalarAsync() as string;

                if (actualTableName == null)
                {
                    _logger.LogDebug("Table {Table} does not exist, skipping column {Column}", tableName, columnName);
                    return;
                }

                try
                {
                    var alterSql = $"ALTER TABLE \"{actualTableName}\" ADD COLUMN \"{columnName}\" {columnType} DEFAULT {defaultValue}";
                    await using var alterCmd = new NpgsqlCommand(alterSql, connection);
                    await alterCmd.ExecuteNonQueryAsync();
                    _logger.LogInformation("Added column {Column} to table {Table}", columnName, actualTableName);
                }
                catch (PostgresException ex) when (ex.SqlState == "42701") // Column already exists
                {
                    _logger.LogDebug("Column {Column} already exists in table {Table}", columnName, actualTableName);
                }
            }
        }

        #endregion
    }
}
