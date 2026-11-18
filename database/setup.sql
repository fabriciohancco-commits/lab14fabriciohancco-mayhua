PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Categories (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT NOT NULL,
    Description TEXT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_Categories_Name ON Categories(Name);

CREATE TABLE IF NOT EXISTS Products (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    Name       TEXT NOT NULL,
    Price      TEXT NOT NULL,
    CategoryId INTEGER NOT NULL,
    CONSTRAINT FK_Products_Categories_CategoryId
        FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE RESTRICT
);

INSERT OR IGNORE INTO Categories (Id, Name, Description) VALUES
    (1, 'Accesorios', 'Accesorios de computación'),
    (2, 'Periféricos', 'Dispositivos de entrada y salida');
