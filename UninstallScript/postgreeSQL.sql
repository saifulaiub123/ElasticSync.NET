-- Drop all triggers starting with 'elastic_sync_'
DO $$
DECLARE
    rec RECORD;
BEGIN
    FOR rec IN
        SELECT tgname, relname
        FROM pg_trigger
        JOIN pg_class ON pg_trigger.tgrelid = pg_class.oid
        WHERE tgname LIKE 'elastic_sync_%' AND NOT tgisinternal
    LOOP
        EXECUTE format('DROP TRIGGER IF EXISTS %I ON %I;', rec.tgname, rec.relname);
    END LOOP;
END;
$$;

-- Drop all functions starting with 'elastic_sync_' (example for one argument of type text)
DO $$
DECLARE
    func RECORD;
BEGIN
    FOR func IN
        SELECT routine_schema, routine_name, routine_type
        FROM information_schema.routines
        WHERE routine_name LIKE 'elastic_sync_%'
    LOOP
        EXECUTE format('DROP FUNCTION IF EXISTS %I.%I(text) CASCADE;', func.routine_schema, func.routine_name);
        -- Adjust arguments list if your functions have different signatures
    END LOOP;
END;
$$;

-- Drop change_log table if it uses your prefix, otherwise:
DROP TABLE IF EXISTS elastic_sync_change_log CASCADE;
