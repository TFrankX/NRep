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
        private const int CURRENT_SCHEMA_VERSION = 10;

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

                // v2: Create AppSettings table
                await MigrateAppSettings_v2_CreateTableAsync(connection);

                // v2: Seed default pricing settings
                await MigrateAppSettings_v2_SeedPricingAsync(connection);

                // v2.1: Fix pricing values (comma to dot issue)
                await MigrateAppSettings_v2_1_FixPricingValuesAsync(connection);

                // v3: Add TotalEarnings column to PowerBank
                await MigratePowerBank_v3_TotalEarningsAsync(connection);

                // v4: Seed default support settings
                await MigrateAppSettings_v4_SeedSupportAsync(connection);

                // v5: Fix LastPutTime for old powerbanks
                await MigratePowerBank_v5_FixLastPutTimeAsync(connection);

                // v6: Seed default scan/polling settings
                await MigrateAppSettings_v6_SeedScanAsync(connection);

                // v7: Create FinancialTransactions table
                await MigrateFinancialTransactions_v7_CreateTableAsync(connection);
                await MigrateFinancialTransactions_v8_AddCardInfoAsync(connection);
                await MigrateFinancialTransactions_v9_AddCardDetailsAsync(connection);

                // v10: Add UserLocation column to Device
                await MigrateDevice_v10_AddUserLocationAsync(connection);
                // v10: Seed default zones
                await MigrateAppSettings_v10_SeedZonesAsync(connection);

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

        private async Task MigratePowerBank_v3_TotalEarningsAsync(NpgsqlConnection connection)
        {
            const string migrationName = "PowerBank_v3_TotalEarnings";

            if (await IsMigrationAppliedAsync(connection, migrationName))
                return;

            _logger.LogInformation("Applying migration: {Migration}", migrationName);

            // Add TotalEarnings column for cumulative earnings tracking
            await AddColumnIfNotExistsAsync(connection, "PowerBank", "TotalEarnings", "real", "0");

            await RecordMigrationAsync(connection, migrationName);
        }

        private async Task MigratePowerBank_v5_FixLastPutTimeAsync(NpgsqlConnection connection)
        {
            const string migrationName = "PowerBank_v5_FixLastPutTime";

            if (await IsMigrationAppliedAsync(connection, migrationName))
                return;

            _logger.LogInformation("Applying migration: {Migration}", migrationName);

            // Update LastPutTime for powerbanks where it was not set (is min date)
            // Set it to LastUpdate if available, otherwise to now
            var sql = @"
                UPDATE ""PowerBank""
                SET ""LastPutTime"" = COALESCE(NULLIF(""LastUpdate"", '0001-01-01'::timestamp), NOW())
                WHERE ""LastPutTime"" IS NULL OR ""LastPutTime"" < '1971-01-01'::timestamp";

            await using var cmd = new NpgsqlCommand(sql, connection);
            var affected = await cmd.ExecuteNonQueryAsync();

            if (affected > 0)
                _logger.LogInformation("Fixed LastPutTime for {Count} powerbanks", affected);

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

        private async Task MigrateAppSettings_v2_CreateTableAsync(NpgsqlConnection connection)
        {
            const string migrationName = "AppSettings_v2_CreateTable";

            if (await IsMigrationAppliedAsync(connection, migrationName))
                return;

            _logger.LogInformation("Applying migration: {Migration}", migrationName);

            var sql = @"
                CREATE TABLE IF NOT EXISTS ""AppSettings"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Category"" VARCHAR(50) NOT NULL,
                    ""Key"" VARCHAR(100) NOT NULL,
                    ""Value"" TEXT NOT NULL DEFAULT '',
                    ""ValueType"" VARCHAR(20) NOT NULL DEFAULT 'string',
                    ""Description"" VARCHAR(255) NOT NULL DEFAULT '',
                    ""DisplayOrder"" INTEGER NOT NULL DEFAULT 0,
                    ""LastModified"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
                    ""ModifiedBy"" VARCHAR(100) NOT NULL DEFAULT '',
                    UNIQUE(""Category"", ""Key"")
                )";

            await using var cmd = new NpgsqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();

            // Create index for faster lookups
            var indexSql = @"CREATE INDEX IF NOT EXISTS ""IX_AppSettings_Category"" ON ""AppSettings"" (""Category"")";
            await using var indexCmd = new NpgsqlCommand(indexSql, connection);
            await indexCmd.ExecuteNonQueryAsync();

            await RecordMigrationAsync(connection, migrationName);
        }

        private async Task MigrateAppSettings_v2_SeedPricingAsync(NpgsqlConnection connection)
        {
            const string migrationName = "AppSettings_v2_SeedPricing";

            if (await IsMigrationAppliedAsync(connection, migrationName))
                return;

            _logger.LogInformation("Applying migration: {Migration}", migrationName);

            // Default pricing plans to seed
            var pricingPlans = new[]
            {
                ("PayByCard", "Standard Plan", 25.0f, 2.0f, 2.0f, 12.0f, 3, "eur"),
                ("PayByCard2", "Plan 2", 25.0f, 2.0f, 2.0f, 12.0f, 3, "eur"),
                ("PayByCard3", "Plan 3", 12.0f, 1.0f, 1.0f, 6.0f, 3, "eur"),
                ("PayByCard4", "Plan 4", 12.0f, 1.0f, 1.0f, 6.0f, 3, "eur")
            };

            int order = 0;
            foreach (var (planName, displayName, hold, baseFee, hourly, daily, maxDays, currency) in pricingPlans)
            {
                await InsertSettingIfNotExistsAsync(connection, "Pricing", $"{planName}.DisplayName", displayName, "string", $"Display name for {planName}", order++);
                await InsertSettingIfNotExistsAsync(connection, "Pricing", $"{planName}.HoldAmount", hold.ToString("F2", System.Globalization.CultureInfo.InvariantCulture), "float", "Hold amount (deposit)", order++);
                await InsertSettingIfNotExistsAsync(connection, "Pricing", $"{planName}.BaseFee", baseFee.ToString("F2", System.Globalization.CultureInfo.InvariantCulture), "float", "Base fee for rental", order++);
                await InsertSettingIfNotExistsAsync(connection, "Pricing", $"{planName}.HourlyRate", hourly.ToString("F2", System.Globalization.CultureInfo.InvariantCulture), "float", "Hourly rate (first day)", order++);
                await InsertSettingIfNotExistsAsync(connection, "Pricing", $"{planName}.DailyRate", daily.ToString("F2", System.Globalization.CultureInfo.InvariantCulture), "float", "Daily rate (after first day)", order++);
                await InsertSettingIfNotExistsAsync(connection, "Pricing", $"{planName}.MaxDaysBeforeCapture", maxDays.ToString(), "int", "Max days before full capture", order++);
                await InsertSettingIfNotExistsAsync(connection, "Pricing", $"{planName}.Currency", currency, "string", "Currency code", order++);
            }

            await RecordMigrationAsync(connection, migrationName);
        }

        private async Task MigrateAppSettings_v2_1_FixPricingValuesAsync(NpgsqlConnection connection)
        {
            const string migrationName = "AppSettings_v2_1_FixPricingValues";

            if (await IsMigrationAppliedAsync(connection, migrationName))
                return;

            _logger.LogInformation("Applying migration: {Migration}", migrationName);

            // Fix float values that were stored with comma instead of dot
            // This happens when ToString("F2") uses local culture with comma
            var fixSql = @"
                UPDATE ""AppSettings""
                SET ""Value"" = REPLACE(""Value"", ',', '.')
                WHERE ""Category"" = 'Pricing'
                AND ""ValueType"" = 'float'
                AND ""Value"" LIKE '%,%'";

            await using var cmd = new NpgsqlCommand(fixSql, connection);
            var affected = await cmd.ExecuteNonQueryAsync();

            if (affected > 0)
                _logger.LogInformation("Fixed {Count} pricing values with comma separator", affected);

            await RecordMigrationAsync(connection, migrationName);
        }

        private async Task MigrateAppSettings_v4_SeedSupportAsync(NpgsqlConnection connection)
        {
            const string migrationName = "AppSettings_v4_SeedSupport";

            if (await IsMigrationAppliedAsync(connection, migrationName))
                return;

            _logger.LogInformation("Applying migration: {Migration}", migrationName);

            // Default support settings
            await InsertSettingIfNotExistsAsync(connection, "Support", "Phone", "+357 99 123 456", "string", "Support phone number", 0);
            await InsertSettingIfNotExistsAsync(connection, "Support", "Email", "support@a-charger.com", "string", "Support email", 1);
            await InsertSettingIfNotExistsAsync(connection, "Support", "WorkingHours", "24/7", "string", "Support working hours", 2);

            await RecordMigrationAsync(connection, migrationName);
        }

        private async Task MigrateAppSettings_v6_SeedScanAsync(NpgsqlConnection connection)
        {
            const string migrationName = "AppSettings_v6_SeedScan";

            if (await IsMigrationAppliedAsync(connection, migrationName))
                return;

            _logger.LogInformation("Applying migration: {Migration}", migrationName);

            // Default scan/polling settings
            await InsertSettingIfNotExistsAsync(connection, "Scan", "InventoryPeriodSeconds", "300", "int", "Interval between inventory requests to each station (seconds)", 0);
            await InsertSettingIfNotExistsAsync(connection, "Scan", "OfflineRetryCount", "3", "int", "Number of retry attempts before marking station offline", 1);
            await InsertSettingIfNotExistsAsync(connection, "Scan", "RetryDelaySeconds", "5", "int", "Delay between retry attempts (seconds)", 2);
            await InsertSettingIfNotExistsAsync(connection, "Scan", "ResponseTimeoutSeconds", "10", "int", "Timeout waiting for station response (seconds)", 3);

            await RecordMigrationAsync(connection, migrationName);
        }

        private async Task MigrateFinancialTransactions_v7_CreateTableAsync(NpgsqlConnection connection)
        {
            const string migrationName = "FinancialTransactions_v7_CreateTable";

            if (await IsMigrationAppliedAsync(connection, migrationName))
                return;

            _logger.LogInformation("Applying migration: {Migration}", migrationName);

            // Create FinancialTransactions table
            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS ""FinancialTransactions"" (
                    ""Id"" BIGSERIAL PRIMARY KEY,
                    ""TransactionTime"" timestamp without time zone NOT NULL,
                    ""Type"" integer NOT NULL,
                    ""Amount"" decimal(10,2) NOT NULL DEFAULT 0,
                    ""StationId"" bigint NOT NULL DEFAULT 0,
                    ""StationName"" text NOT NULL DEFAULT '',
                    ""PowerBankId"" bigint NOT NULL DEFAULT 0,
                    ""UserId"" text NOT NULL DEFAULT '',
                    ""CustomerName"" text NOT NULL DEFAULT '',
                    ""PaymentReference"" text NOT NULL DEFAULT '',
                    ""SessionId"" text NOT NULL DEFAULT '',
                    ""Description"" text NOT NULL DEFAULT ''
                )";

            await using var createCmd = new NpgsqlCommand(createTableSql, connection);
            await createCmd.ExecuteNonQueryAsync();
            _logger.LogInformation("Created table FinancialTransactions");

            // Create indexes
            var indexSqls = new[]
            {
                @"CREATE INDEX IF NOT EXISTS ""IX_FinancialTransactions_TransactionTime"" ON ""FinancialTransactions"" (""TransactionTime"")",
                @"CREATE INDEX IF NOT EXISTS ""IX_FinancialTransactions_StationId"" ON ""FinancialTransactions"" (""StationId"")",
                @"CREATE INDEX IF NOT EXISTS ""IX_FinancialTransactions_Type"" ON ""FinancialTransactions"" (""Type"")"
            };

            foreach (var indexSql in indexSqls)
            {
                await using var indexCmd = new NpgsqlCommand(indexSql, connection);
                await indexCmd.ExecuteNonQueryAsync();
            }
            _logger.LogInformation("Created indexes for FinancialTransactions");

            await RecordMigrationAsync(connection, migrationName);
        }

        private async Task MigrateFinancialTransactions_v8_AddCardInfoAsync(NpgsqlConnection connection)
        {
            const string migrationName = "FinancialTransactions_v8_AddCardInfo";

            if (await IsMigrationAppliedAsync(connection, migrationName))
                return;

            _logger.LogInformation("Applying migration: {Migration}", migrationName);

            await AddColumnIfNotExistsAsync(connection, "FinancialTransactions", "CardInfo", "text", "''");

            await RecordMigrationAsync(connection, migrationName);
        }

        private async Task MigrateFinancialTransactions_v9_AddCardDetailsAsync(NpgsqlConnection connection)
        {
            const string migrationName = "FinancialTransactions_v9_AddCardDetails";

            if (await IsMigrationAppliedAsync(connection, migrationName))
                return;

            _logger.LogInformation("Applying migration: {Migration}", migrationName);

            await AddColumnIfNotExistsAsync(connection, "FinancialTransactions", "CardExpiry", "text", "''");
            await AddColumnIfNotExistsAsync(connection, "FinancialTransactions", "CardCountry", "text", "''");

            await RecordMigrationAsync(connection, migrationName);
        }

        private async Task MigrateDevice_v10_AddUserLocationAsync(NpgsqlConnection connection)
        {
            const string migrationName = "Device_v10_AddUserLocation";

            if (await IsMigrationAppliedAsync(connection, migrationName))
                return;

            _logger.LogInformation("Applying migration: {Migration}", migrationName);

            await AddColumnIfNotExistsAsync(connection, "Device", "UserLocation", "text", "''");

            await RecordMigrationAsync(connection, migrationName);
        }

        private async Task MigrateAppSettings_v10_SeedZonesAsync(NpgsqlConnection connection)
        {
            const string migrationName = "AppSettings_v10_SeedZones";

            if (await IsMigrationAppliedAsync(connection, migrationName))
                return;

            _logger.LogInformation("Applying migration: {Migration}", migrationName);

            // Create default zone
            await InsertSettingIfNotExistsAsync(connection, "Zones", "1.Name", "Default", "string", "Zone name", 0);
            await InsertSettingIfNotExistsAsync(connection, "Zones", "1.Color", "#7C3AED", "string", "Zone color", 1);
            await InsertSettingIfNotExistsAsync(connection, "Zones", "1.Language", "en", "string", "Zone language", 2);

            await RecordMigrationAsync(connection, migrationName);
        }

        private async Task InsertSettingIfNotExistsAsync(NpgsqlConnection connection, string category, string key, string value, string valueType, string description, int displayOrder)
        {
            var sql = @"
                INSERT INTO ""AppSettings"" (""Category"", ""Key"", ""Value"", ""ValueType"", ""Description"", ""DisplayOrder"", ""LastModified"", ""ModifiedBy"")
                VALUES (@category, @key, @value, @valueType, @description, @displayOrder, NOW(), 'system')
                ON CONFLICT (""Category"", ""Key"") DO NOTHING";

            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("category", category);
            cmd.Parameters.AddWithValue("key", key);
            cmd.Parameters.AddWithValue("value", value);
            cmd.Parameters.AddWithValue("valueType", valueType);
            cmd.Parameters.AddWithValue("description", description);
            cmd.Parameters.AddWithValue("displayOrder", displayOrder);
            await cmd.ExecuteNonQueryAsync();
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
