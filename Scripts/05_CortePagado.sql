-- Función: marcar como pagado el último corte de una tarjeta ("ya pagué")
-- Ejecutar una vez en FinanzasDB (SSMS) ANTES de iniciar la API actualizada.
-- CortePagado = fecha del corte cuyo pago ya se hizo; el aviso de pago se oculta mientras CortePagado >= último corte.

IF COL_LENGTH('dbo.Cuenta', 'CortePagado') IS NULL
BEGIN
    ALTER TABLE dbo.Cuenta ADD CortePagado date NULL;
END
GO
