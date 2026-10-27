-- Función: categorías propias de cada usuario
-- Ejecutar una vez en FinanzasDB (SSMS) ANTES de iniciar la API actualizada.
-- UsuarioId NULL = categoría general (de todos); con valor = categoría privada de ese usuario.

IF COL_LENGTH('dbo.Categoria', 'UsuarioId') IS NULL
BEGIN
    ALTER TABLE dbo.Categoria ADD UsuarioId int NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Categoria_Usuario')
BEGIN
    ALTER TABLE dbo.Categoria ADD CONSTRAINT FK_Categoria_Usuario FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuario (Id);
END
GO

-- La unicidad (Nombre, TipoSugerido) era global; ahora es por usuario (las generales siguen siendo únicas entre sí).
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_Categoria_Nombre_Tipo' AND parent_object_id = OBJECT_ID('dbo.Categoria'))
    ALTER TABLE dbo.Categoria DROP CONSTRAINT UQ_Categoria_Nombre_Tipo;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Categoria_Nombre_Tipo' AND object_id = OBJECT_ID('dbo.Categoria'))
    DROP INDEX UQ_Categoria_Nombre_Tipo ON dbo.Categoria;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Categoria_Usuario_Nombre_Tipo' AND object_id = OBJECT_ID('dbo.Categoria'))
    CREATE UNIQUE INDEX UQ_Categoria_Usuario_Nombre_Tipo ON dbo.Categoria (UsuarioId, Nombre, TipoSugerido);
GO
