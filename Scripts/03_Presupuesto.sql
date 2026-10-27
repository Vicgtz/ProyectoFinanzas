-- Función: presupuestos mensuales por categoría
-- Ejecutar una vez en FinanzasDB (SSMS) ANTES de iniciar la API actualizada.
-- Un presupuesto es un monto fijo mensual por categoría; aplica a todos los meses hasta que lo cambies o lo borres.

IF OBJECT_ID('dbo.Presupuesto', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Presupuesto
    (
        Id                  int IDENTITY(1,1) NOT NULL,
        UsuarioId           int               NOT NULL,
        CategoriaId         int               NOT NULL,
        Monto               decimal(18,2)     NOT NULL,
        FechaCreacion       datetime2         NOT NULL CONSTRAINT DF_Presupuesto_FechaCreacion DEFAULT (SYSUTCDATETIME()),
        FechaActualizacion  datetime2         NULL,
        CONSTRAINT PK_Presupuesto PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Presupuesto_Usuario   FOREIGN KEY (UsuarioId)   REFERENCES dbo.Usuario (Id),
        CONSTRAINT FK_Presupuesto_Categoria FOREIGN KEY (CategoriaId) REFERENCES dbo.Categoria (Id),
        CONSTRAINT CK_Presupuesto_Monto CHECK (Monto > 0),
        CONSTRAINT UQ_Presupuesto_Usuario_Categoria UNIQUE (UsuarioId, CategoriaId)
    );
END
GO
