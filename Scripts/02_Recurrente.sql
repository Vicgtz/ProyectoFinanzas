-- Función: operaciones recurrentes (renta, suscripciones, nómina...)
-- Ejecutar una vez en FinanzasDB (SSMS) ANTES de iniciar la API actualizada.
-- Frecuencia: S = semanal, Q = quincenal (días 15 y último del mes), M = mensual, A = anual.
-- ProximaFecha = fecha de la siguiente ocurrencia por confirmar. Al llegar esa fecha aparece como pendiente.

IF OBJECT_ID('dbo.Recurrente', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Recurrente
    (
        Id              int IDENTITY(1,1) NOT NULL,
        UsuarioId       int               NOT NULL,
        TipoOperacion   varchar(30)       NOT NULL,
        Descripcion     nvarchar(250)     NOT NULL,
        Monto           decimal(18,2)     NOT NULL,
        CategoriaId     int               NULL,
        CuentaId        int               NOT NULL,
        Frecuencia      char(1)           NOT NULL,
        FechaInicio     date              NOT NULL,
        ProximaFecha    date              NOT NULL,
        FechaFin        date              NULL,
        Activa          bit               NOT NULL CONSTRAINT DF_Recurrente_Activa DEFAULT (1),
        FechaCreacion   datetime2         NOT NULL CONSTRAINT DF_Recurrente_FechaCreacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Recurrente PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Recurrente_Usuario   FOREIGN KEY (UsuarioId)   REFERENCES dbo.Usuario (Id),
        CONSTRAINT FK_Recurrente_Categoria FOREIGN KEY (CategoriaId) REFERENCES dbo.Categoria (Id),
        CONSTRAINT FK_Recurrente_Cuenta    FOREIGN KEY (CuentaId)    REFERENCES dbo.Cuenta (Id),
        CONSTRAINT CK_Recurrente_Tipo       CHECK (TipoOperacion IN ('Gasto', 'Ingreso')),
        CONSTRAINT CK_Recurrente_Monto      CHECK (Monto > 0),
        CONSTRAINT CK_Recurrente_Frecuencia CHECK (Frecuencia IN ('S', 'Q', 'M', 'A'))
    );

    CREATE INDEX IX_Recurrente_Usuario_Proxima ON dbo.Recurrente (UsuarioId, Activa, ProximaFecha);
END
GO
