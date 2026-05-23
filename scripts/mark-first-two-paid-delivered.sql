-- Uso:
-- 1. Substitua o valor abaixo pelo Id do plano desejado.
-- 2. Execute o script no PostgreSQL.

\set plan_id '7b889437-dc9f-4435-a2ef-9c66e244e3eb'

BEGIN;

-- Estado atual das 2 primeiras entregas
SELECT "Id", "PlanId", "Period", "DueDate", "DeliveryDate", "Status"
FROM "Deliveries"
WHERE "PlanId" = :'plan_id'::uuid
ORDER BY "Period"
LIMIT 2;

-- Estado atual das 2 primeiras mensalidades
SELECT "Id", "PlanId", "Period", "DueDate", "PaidAt", "ReceivedBy", "Status"
FROM "Billings"
WHERE "PlanId" = :'plan_id'::uuid
ORDER BY "Period"
LIMIT 2;

WITH first_two_deliveries AS (
    SELECT "Id"
    FROM "Deliveries"
    WHERE "PlanId" = :'plan_id'::uuid
    ORDER BY "Period"
    LIMIT 2
)
UPDATE "Deliveries" d
SET
    "Status" = 'Delivered',
    "DeliveryDate" = COALESCE(d."DeliveryDate", NOW())
FROM first_two_deliveries f
WHERE d."Id" = f."Id"
RETURNING d."Id", d."Period", d."Status", d."DeliveryDate";

WITH first_two_billings AS (
    SELECT "Id"
    FROM "Billings"
    WHERE "PlanId" = :'plan_id'::uuid
    ORDER BY "Period"
    LIMIT 1
)
UPDATE "Billings" b
SET
    "Status" = 'Paid',
    "PaidAt" = COALESCE(b."PaidAt", NOW())
FROM first_two_billings f
WHERE b."Id" = f."Id"
RETURNING b."Id", b."Period", b."Status", b."PaidAt", b."ReceivedBy";

-- Estado final das 2 primeiras entregas
SELECT "Id", "PlanId", "Period", "DueDate", "DeliveryDate", "Status"
FROM "Deliveries"
WHERE "PlanId" = :'plan_id'::uuid
ORDER BY "Period"
LIMIT 2;

-- Estado final das 2 primeiras mensalidades
SELECT "Id", "PlanId", "Period", "DueDate", "PaidAt", "ReceivedBy", "Status"
FROM "Billings"
WHERE "PlanId" = :'plan_id'::uuid
ORDER BY "Period"
LIMIT 2;

COMMIT;
