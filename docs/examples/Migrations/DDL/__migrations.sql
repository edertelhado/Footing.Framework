-- DDL agnóstico __migrations — Y/N CHAR(1) 100% agnóstico (PG CHAR(1), MSSQL CHAR(1), Firebird CHAR(1), dbf C(1), SQLite TEXT)
-- success CHAR(1) Y/N CHECK (success IN ('Y','N')) — vence BOOLEAN/BIT/INTEGER 0/1 (GLOBAL_RULES §1)
CREATE TABLE __migrations (
    id INTEGER PRIMARY KEY,
    version VARCHAR(50) NOT NULL,
    description VARCHAR(200),
    type VARCHAR(20),        -- 'BASELINE' | 'VERSIONED' | 'REPEATABLE'
    checksum VARCHAR(64),    -- SHA-256 hex
    installed_on TIMESTAMP,
    execution_time_ms INTEGER,
    success CHAR(1) NOT NULL CHECK (success IN ('Y','N')),
    error_message VARCHAR(4000)
);

CREATE INDEX idx_migrations_version ON __migrations(version);
CREATE INDEX idx_migrations_success ON __migrations(success);

-- Inserts agnósticos Y/N
-- INSERT INTO __migrations (version, description, type, checksum, installed_on, execution_time_ms, success, error_message) VALUES ('V001','create_users','VERSIONED','abc123...', CURRENT_TIMESTAMP, 120, 'Y', NULL);
-- SELECT * FROM __migrations WHERE success = 'Y'; -- sucesso
-- SELECT * FROM __migrations WHERE success = 'N'; -- falha pendente bloqueia Migrate()
-- DELETE FROM __migrations WHERE success = 'N'; -- Repair()

-- DDL por DB (copiar):
-- PG/MSSQL/Firebird: idem acima (CHAR(1) CHECK funciona)
-- dbf FoxPro: CREATE TABLE __migrations (id N(10,0), version C(50), description C(200), type C(20), checksum C(64), installed_on D, execution_time_ms N(10,0), success C(1), error_message M)
