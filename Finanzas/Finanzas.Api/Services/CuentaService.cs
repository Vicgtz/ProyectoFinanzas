using Finanzas.Data;
using Finanzas.Models;
using Finanzas.Models.Cuenta;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Api.Services;

public class CuentaService(FinanzasDbContext context)
{
    public async Task<BaseResponse<CuentaModel>> Get(int idUsuario, int idCuenta)
    {
        var cuenta = await context.Cuentas.AsNoTracking()
            .Where(a => a.Id == idCuenta && a.UsuarioId == idUsuario)
            .Select(a => ToModel(a))
            .FirstOrDefaultAsync();

        if (cuenta == null) return new BaseResponse<CuentaModel> { Message = "Cuenta no encontrada" };
        return new BaseResponse<CuentaModel> { Success = true, Data = cuenta };
    }

    public async Task<BaseResponse<List<CuentaModel>>> GetList(int idUsuario, bool soloActivas = false)
    {
        var cuentas = await context.Cuentas.AsNoTracking()
            .Where(a => a.UsuarioId == idUsuario && (!soloActivas || a.Activa))
            .OrderBy(a => a.Nombre)
            .Select(a => ToModel(a))
            .ToListAsync();

        return new BaseResponse<List<CuentaModel>> { Success = true, Data = cuentas };
    }

    public async Task<BaseResponse> Save(int idUsuario, CuentaModel cuenta)
    {
        var check = Validar(cuenta);
        if (!check.Success) return check;

        var nombre = cuenta.Nombre.Trim();
        var existe = await context.Cuentas.AnyAsync(a => a.UsuarioId == idUsuario && a.Nombre == nombre && a.TipoCuenta == cuenta.TipoCuenta);
        if (existe) return new BaseResponse { Message = "Cuenta ya existe" };

        var nueva = new Cuenta
        {
            UsuarioId = idUsuario,
            Nombre = nombre,
            TipoCuenta = cuenta.TipoCuenta,
            Saldo = cuenta.Saldo,
            LimiteCredito = cuenta.LimiteCredito,
            DiaCorte = cuenta.DiaCorte,
            DiasParaPago = cuenta.DiasParaPago,
            Activa = true
        };

        context.Cuentas.Add(nueva);
        await context.SaveChangesAsync();
        return new BaseResponse { Success = true, Message = "Guardado", Id = nueva.Id };
    }

    // El saldo (o deuda) puede corregirse manualmente; Validar ya revisa que no supere el límite.
    public async Task<BaseResponse> Edit(int idUsuario, CuentaModel cuenta)
    {
        var check = Validar(cuenta);
        if (!check.Success) return check;

        var actual = await context.Cuentas.FirstOrDefaultAsync(a => a.Id == cuenta.Id && a.UsuarioId == idUsuario);
        if (actual == null) return new BaseResponse { Message = "Cuenta no existe" };

        if (actual.TipoCuenta != cuenta.TipoCuenta)
            return new BaseResponse { Message = "No se puede cambiar el tipo de cuenta" };

        var nombre = cuenta.Nombre.Trim();
        var duplicada = await context.Cuentas.AnyAsync(a => a.Id != actual.Id && a.UsuarioId == idUsuario && a.Nombre == nombre && a.TipoCuenta == actual.TipoCuenta);
        if (duplicada) return new BaseResponse { Message = "Cuenta ya existe" };

        actual.Nombre = nombre;
        actual.Activa = cuenta.Activa;
        actual.Saldo = cuenta.Saldo;
        actual.LimiteCredito = cuenta.LimiteCredito;
        actual.DiaCorte = cuenta.DiaCorte;
        actual.DiasParaPago = cuenta.DiasParaPago;

        await context.SaveChangesAsync();
        return new BaseResponse { Success = true, Message = "Guardado", Id = actual.Id };
    }

    private static CuentaModel ToModel(Cuenta a) => new()
    {
        Id = a.Id,
        Nombre = a.Nombre,
        TipoCuenta = a.TipoCuenta,
        Saldo = a.Saldo,
        LimiteCredito = a.LimiteCredito,
        DiaCorte = a.DiaCorte,
        DiasParaPago = a.DiasParaPago,
        Activa = a.Activa
    };

    // Replica las reglas CHECK de la tabla Cuenta.
    private static BaseResponse Validar(CuentaModel c)
    {
        if (string.IsNullOrWhiteSpace(c.Nombre)) return new BaseResponse { Message = "Nombre vacío" };
        if (c.Nombre.Trim().Length > 100) return new BaseResponse { Message = "Nombre supera los caracteres permitidos" };
        if (c.TipoCuenta is not (TipoCuentaItem.Efectivo or TipoCuentaItem.Debito or TipoCuentaItem.Credito))
            return new BaseResponse { Message = "Tipo de cuenta inválido" };
        if (c.Saldo < 0) return new BaseResponse { Message = "El saldo no puede ser negativo" };

        if (c.TipoCuenta == TipoCuentaItem.Credito)
        {
            if (c.LimiteCredito is null or < 0) return new BaseResponse { Message = "Límite de crédito inválido" };
            if (c.DiaCorte is null or < 1 or > 31) return new BaseResponse { Message = "El día de corte debe estar entre 1 y 31" };
            if (c.DiasParaPago is < 1 or > 60) return new BaseResponse { Message = "Los días para pago deben estar entre 1 y 60" };
            if (c.Saldo > c.LimiteCredito) return new BaseResponse { Message = "El saldo no puede superar el límite de crédito" };
        }
        else
        {
            if (c.LimiteCredito != null) return new BaseResponse { Message = "Solo las cuentas de crédito tienen límite" };
            if (c.DiaCorte != null) return new BaseResponse { Message = "Solo las cuentas de crédito tienen día de corte" };
            if (c.DiasParaPago != null) return new BaseResponse { Message = "Solo las cuentas de crédito tienen días para pago" };
        }

        return new BaseResponse { Success = true };
    }
}
