using Npgsql;
using System.Text;
using ChangeSync.Elastic.Postgres.Models;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Diagnostics;

namespace ChangeSync.Elastic.Postgres.Services;

public class ChangeLogInstaller
{
    private readonly ChangeSyncOptions _options;
    private readonly string namingPrefix = "elastic_sync_";

    public ChangeLogInstaller(ChangeSyncOptions options)
    {
        _options = options;
    }

    public async Task InstallAsync()
    {
        try
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            await using var conn = new NpgsqlConnection(_options.PostgresConnectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = BuildInstallScript(conn);
            await cmd.ExecuteNonQueryAsync();

            sw.Stop();
            Console.Write($"times taken to create 30 triggers :  {sw.Elapsed.Duration().TotalMilliseconds} ms");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private string BuildInstallScript(NpgsqlConnection conn)
    {
        var sb = new StringBuilder();

        // Drop old table
        sb.AppendLine($@"
            DROP TABLE IF EXISTS change_log CASCADE;
        ");

        // Drop old function
        sb.AppendLine($@"
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM pg_proc 
                    WHERE proname = 'log_change' AND pronargs = 0
                ) THEN
                    DROP FUNCTION log_change() CASCADE;
                END IF;
            END $$;
        ");

        sb.AppendLine($@"
            DO $$
                DECLARE
                    namingPrefix TEXT := 'elastic_sync_';
                    schemaName TEXT := 'esnet';
                    tableName TEXT := namingPrefix || 'change_log';
                BEGIN
                    EXECUTE format('CREATE SCHEMA IF NOT EXISTS %I', schemaName);
                END$$;
        ");

        sb.AppendLine($@"
            CREATE TABLE IF NOT EXISTS esnet.{namingPrefix}change_log (
                id SERIAL PRIMARY KEY,
                table_name TEXT NOT NULL,
                operation TEXT NOT NULL,
                record_id TEXT NOT NULL,
                payload JSONB NOT NULL,
                processed BOOLEAN DEFAULT FALSE,
                retry_count INT DEFAULT 0,
                last_error TEXT,
                dead_letter BOOLEAN DEFAULT FALSE,
                locked_by TEXT,
                locked_at TIMESTAMP,
                next_retry_at TIMESTAMP,
                last_attempt_at TIMESTAMP;
                created_at TIMESTAMP DEFAULT now()
            );");

        sb.AppendLine(CreateIndexIfNotExist());

        sb.AppendLine($@"
            CREATE OR REPLACE FUNCTION {namingPrefix}log_change() 
            RETURNS trigger
            AS $$
            DECLARE
                pk_column text := TG_ARGV[0];
                rec_id TEXT;
            BEGIN

                EXECUTE format('SELECT ($1).%I', pk_column)
                USING COALESCE(NEW, OLD)
                INTO rec_id;

                INSERT INTO esnet.{namingPrefix}change_log (table_name, operation, record_id, payload)
                VALUES (
                    TG_TABLE_NAME,
                    TG_OP,
                    rec_id,
                    row_to_json(COALESCE(NEW, OLD))
                );
                PERFORM pg_notify('{namingPrefix}change_log_channel', 'new_change');
                RETURN NULL;
            END;
            $$ LANGUAGE plpgsql;");

        foreach (var entity in _options.Entities)
        {
            var table = entity.Table;
            var pkName = GetPrimaryKeyName(conn, table);
            var triggerName = $"trg_log_{table.ToLower()}";

            foreach (var action in new[] { "INSERT", "UPDATE", "DELETE" })
            {
                string oldTriggerName = $"{triggerName}_{action.ToLower()}";
                string newTriggerName = $"{namingPrefix}{triggerName}_{action.ToLower()}";

                //DROP TRIGGER
                sb.AppendLine($@"
                    DO $$
                    BEGIN
                        IF EXISTS (
                            SELECT 1 FROM pg_trigger t
                                JOIN pg_class c ON t.tgrelid = c.oid
                                WHERE t.tgname = '{oldTriggerName}'
                                AND c.relname = '{table}'  
                        ) THEN
                            DROP TRIGGER IF EXISTS {oldTriggerName} ON {table};
                        END IF;
                    END $$;
                ");


                sb.AppendLine($@"
                DO $$
                BEGIN
                    
                    IF NOT EXISTS (
                    SELECT 1 FROM pg_trigger t
                    JOIN pg_class c ON t.tgrelid = c.oid
                    WHERE t.tgname = '{newTriggerName}'
                        AND c.relname = '{table}'                
                    ) THEN
                    CREATE TRIGGER {newTriggerName}
                    AFTER {action} ON {QuoteIfNeeded(table)}
                    FOR EACH ROW EXECUTE FUNCTION {namingPrefix}log_change('{pkName}');
                    END IF;
                END;
                $$;");   
            }
        }
        Console.WriteLine(sb.ToString());
        return sb.ToString();
    }
    private string CreateIndexIfNotExist()
    {
        var query = $@"
            DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_indexes
                        WHERE schemaname = 'esnet'
                            AND tablename = 'elastic_sync_change_log'
                            AND indexname = 'elastic_sync_change_log_processed_idx'
                    ) THEN
                        EXECUTE format(
                            'CREATE INDEX %I ON %I.%I (processed)',
                            'elastic_sync_change_log_processed_idx',
                            'esnet',
                            'elastic_sync_change_log'
                        );
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_indexes
                        WHERE schemaname = 'esnet'
                            AND tablename = 'elastic_sync_change_log'
                            AND indexname = 'elastic_sync_change_log_record_id_idx'
                    ) THEN
                        EXECUTE format(
                            'CREATE INDEX %I ON %I.%I (record_id)',
                            'elastic_sync_change_log_record_id_idx',
                            'esnet',
                            'elastic_sync_change_log'
                        );
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_indexes
                        WHERE schemaname = 'esnet'
                            AND tablename = 'elastic_sync_change_log'
                            AND indexname = 'elastic_sync_change_log_operation_idx'
                    ) THEN
                        EXECUTE format(
                            'CREATE INDEX %I ON %I.%I (operation)',
                            'elastic_sync_change_log_operation_idx',
                            'esnet',
                            'elastic_sync_change_log'
                        );
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_indexes
                        WHERE schemaname = 'esnet'
                            AND tablename = 'elastic_sync_change_log'
                            AND indexname = 'elastic_sync_change_log_dead_letter_idx'
                    ) THEN
                        EXECUTE format(
                            'CREATE INDEX %I ON %I.%I (dead_letter)',
                            'elastic_sync_change_log_dead_letter_idx',
                            'esnet',
                            'elastic_sync_change_log'
                        );
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_indexes
                        WHERE schemaname = 'esnet'
                            AND tablename = 'elastic_sync_change_log'
                            AND indexname = 'elastic_sync_change_log_created_at_idx'
                    ) THEN
                        EXECUTE format(
                            'CREATE INDEX %I ON %I.%I (created_at)',
                            'elastic_sync_change_log_created_at_idx',
                            'esnet',
                            'elastic_sync_change_log'
                        );
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_indexes
                        WHERE schemaname = 'esnet'
                            AND tablename = 'elastic_sync_change_log'
                            AND indexname = 'elastic_sync_change_log_processed_dead_idx'
                    ) THEN
                        EXECUTE format(
                            'CREATE INDEX %I ON %I.%I (processed, dead_letter, id)',
                            'elastic_sync_change_log_processed_dead_idx',
                            'esnet',
                            'elastic_sync_change_log'
                        );
                    END IF;

                    CREATE INDEX IF NOT EXISTS elastic_sync_change_log_unprocessed_idx ON esnet.elastic_sync_change_log(processed, dead_letter, next_retry_at, created_at);
                END
                $$;
            ";
        return query;
    }
    string GetPrimaryKeyName(NpgsqlConnection conn, string table)
    {
        using var cmd = new NpgsqlCommand(@"
        SELECT kcu.column_name
        FROM information_schema.table_constraints tc
        JOIN information_schema.key_column_usage kcu
          ON tc.constraint_name = kcu.constraint_name
         AND tc.table_schema = kcu.table_schema
        WHERE tc.constraint_type = 'PRIMARY KEY'
          AND tc.table_name = @table
        LIMIT 1;", conn);

        cmd.Parameters.AddWithValue("table", table);
        return (string?)cmd.ExecuteScalar() ?? "id";
    }
    string QuoteIfNeeded(string name) => name.Any(char.IsUpper) ? $"\"{name}\"" : name;

    
}