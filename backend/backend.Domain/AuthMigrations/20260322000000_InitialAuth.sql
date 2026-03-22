CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;
CREATE TABLE app_users (
    id uuid NOT NULL,
    subject character varying(64) NOT NULL,
    username character varying(100) NOT NULL,
    email character varying(200),
    created_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_app_users PRIMARY KEY (id)
);

CREATE UNIQUE INDEX ix_app_users_subject ON app_users (subject);

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20260311172642_CreateAuthUserStore', '10.0.1');

COMMIT;

START TRANSACTION;
INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20260322180639_InitialAuth', '10.0.1');

COMMIT;

