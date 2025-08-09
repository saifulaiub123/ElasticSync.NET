using Npgsql;
using System.Text;
using ChangeSync.Elastic.Postgres.Models;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace ChangeSync.Elastic.Postgres.Services;

public class ChangeLogInstaller
{
    private readonly ChangeSyncOptions _options;

    public ChangeLogInstaller(ChangeSyncOptions options)
    {
        _options = options;
    }

    public async Task InstallAsync()
    {
        await using var conn = new NpgsqlConnection(_options.PostgresConnectionString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = BuildInstallScript(conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private string BuildInstallScript(NpgsqlConnection conn)
    {
        var sb = new StringBuilder();

        sb.AppendLine(@"
            CREATE TABLE IF NOT EXISTS change_log (
                id SERIAL PRIMARY KEY,
                table_name TEXT NOT NULL,
                operation TEXT NOT NULL,
                record_id TEXT NOT NULL,
                payload JSONB NOT NULL,
                processed BOOLEAN DEFAULT FALSE,
                retry_count INT DEFAULT 0,
                last_error TEXT,
                dead_letter BOOLEAN DEFAULT FALSE,
                created_at TIMESTAMP DEFAULT now()
            );");

        sb.AppendLine(@"
            CREATE OR REPLACE FUNCTION log_change() 
            RETURNS trigger
            AS $$
            DECLARE
                pk_column text := TG_ARGV[0];
                rec_id TEXT;
            BEGIN

                EXECUTE format('SELECT ($1).%I', pk_column)
                USING COALESCE(NEW, OLD)
                INTO rec_id;

                INSERT INTO change_log (table_name, operation, record_id, payload)
                VALUES (
                    TG_TABLE_NAME,
                    TG_OP,
                    rec_id,
                    row_to_json(COALESCE(NEW, OLD))
                );
                PERFORM pg_notify('change_log_channel', 'new_change');
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;");

        foreach (var entity in _options.Entities)
        {
            var table = entity.Table;
            var pkName = GetPrimaryKeyName(conn, table);
            var triggerName = $"trg_log_{table}";

            foreach (var action in new[] { "INSERT", "UPDATE", "DELETE" })
            {
                sb.AppendLine($@"
                DROP TRIGGER IF EXISTS {triggerName}_{action.ToLower()} ON {QuoteIfNeeded(table)};
                CREATE TRIGGER {triggerName}_{action.ToLower()}
                AFTER {action} ON {QuoteIfNeeded(table)}
                FOR EACH ROW EXECUTE FUNCTION log_change('{pkName}');");
            }
        }

        var t =  sb.ToString();
        Console.WriteLine(t);
        return t;
    }
    string QuoteIfNeeded(string name) => name.Any(char.IsUpper) ? $"\"{name}\"" : name;

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
}