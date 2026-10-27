-- Función: fecha límite de pago de tarjeta de crédito
-- Ejecutar una vez en la base FinanzasDB (SSMS) ANTES de iniciar la API actualizada.
-- DiasParaPago = días después del corte para pagar sin intereses. NULL = sin configurar (no se muestran avisos).

IF COL_LENGTH('dbo.Cuenta', 'DiasParaPago') IS NULL
BEGIN
    ALTER TABLE dbo.Cuenta ADD DiasParaPago tinyint NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Cuenta_DiasParaPago')
BEGIN
    ALTER TABLE dbo.Cuenta ADD CONSTRAINT CK_Cuenta_DiasParaPago
        CHECK (DiasParaPago IS NULL OR (TipoCuenta = 'C' AND DiasParaPago BETWEEN 1 AND 60));
END
GO
