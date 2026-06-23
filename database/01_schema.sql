-- =====================================================================
--  Deep Search - Database Schema (PostgreSQL)
--  הקובץ הזה יוצר את מבנה הטבלאות של המערכת.
--  הוא רץ אוטומטית כשמקימים את ה-DB ב-Docker בפעם הראשונה.
-- =====================================================================

-- ---------------------------------------------------------------------
-- טבלאות ממד (Metadata) - מזינות את התפריטים בבונה השאילתות
-- ---------------------------------------------------------------------

CREATE TABLE cities (
    id   SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE sectors (
    id   SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE
);

-- ---------------------------------------------------------------------
-- טבלת העובדות (Fact Table) - הנתונים עצמם
-- כל שורה = תצפית על אדם אחד בשנה אחת
-- ---------------------------------------------------------------------

CREATE TABLE population_records (
    id             BIGSERIAL     PRIMARY KEY,
    gender         VARCHAR(10)   NOT NULL,                       -- 'male' / 'female'
    age            INT           NOT NULL,
    city_id        INT           NOT NULL REFERENCES cities(id),
    sector_id      INT           NOT NULL REFERENCES sectors(id),
    year           INT           NOT NULL,
    monthly_income NUMERIC(10,2) NOT NULL,                       -- שכר חודשי
    is_employed    BOOLEAN       NOT NULL                        -- מועסק / לא מועסק
);

-- אינדקסים על העמודות שלפיהן מסננים ומפלחים - לשיפור ביצועי השאילתות
CREATE INDEX idx_pop_year   ON population_records(year);
CREATE INDEX idx_pop_city   ON population_records(city_id);
CREATE INDEX idx_pop_gender ON population_records(gender);
CREATE INDEX idx_pop_sector ON population_records(sector_id);

-- ---------------------------------------------------------------------
-- טבלת השאילתות השמורות (דרישה 4)
-- שומרים את הגדרת השאילתה כ-JSON, כדי שנוכל להריץ אותה מחדש
-- ---------------------------------------------------------------------

CREATE TABLE saved_queries (
    id         SERIAL       PRIMARY KEY,
    name       VARCHAR(200) NOT NULL,
    definition JSONB        NOT NULL,                            -- ה-QueryDefinition כ-JSON
    created_at TIMESTAMP    NOT NULL DEFAULT now()
);
