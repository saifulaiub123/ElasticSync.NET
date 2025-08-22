CREATE USER elastic_sync_rep_usr WITH  
    PASSWORD 'Pass@123'  
    REPLICATION;
	

CREATE PUBLICATION elastic_sync_pub
    FOR TABLE public."Customers", public."Orders"
    WITH (publish = 'insert, update, delete, truncate', publish_via_partition_root = false);

--Create replication slot
SELECT * FROM pg_create_logical_replication_slot('elastic_sync_slot', 'pgoutput');	

--delete replication slot
-- SELECT * FROM pg_drop_replication_slot('my_slot');