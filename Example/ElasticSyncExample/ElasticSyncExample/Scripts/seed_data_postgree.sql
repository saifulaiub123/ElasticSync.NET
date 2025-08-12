DO $$
BEGIN
    FOR i IN 1..1000 LOOP
        -- Insert one customer
        INSERT INTO "Customers" ("Name", "Email")
        VALUES ('Customer ' || i, 'customer' || i || '@example.com');

        -- Insert one product
        INSERT INTO "Products" ("Name", "Price", "CategoryId", "SupplierId")
        VALUES ('Product ' || i, round((random() * 100 + 1)::numeric, 2), 1, 1);
    END LOOP;
END $$;


Delete from "Customers";
Delete from "Products";
Delete from elastic_sync_change_log