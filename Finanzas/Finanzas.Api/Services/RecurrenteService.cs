using Finanzas.Data;
using Finanzas.Models;
using Finanzas.Models.Cuenta;
using Finanzas.Models.Operacion;
using Finanzas.Models.Recurrente;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Api.Services;

// Las recurrentes no mueven saldos: solo generan "pendientes". Al confirmar se crea una operación normal
// con OperacionService (mismas reglas de saldo) y, en el mismo guardado, se avanza la próxima fecha.
public class RecurrenteService(FinanzasDbContext context, OperacionService operaciones)
{
    private const int MaxAtrasadas = 60;

    public async Task<BaseResponse<List<RecurrenteModel>>> GetList(int idUsuario, bool soloActivas = false)
    {
        var lista = await context.Recurrentes.AsNoTracking()
            .Where(a => a.UsuarioId == idUsuario && (!soloActivas || a.Activa))
            .OrderBy(a => a.ProximaFecha).ThenBy(a => a.Descripcion)
            .Select(a => new RecurrenteModel
            {
                Id = a.Id,
                TipoOperacion = a.TipoOperacion,
                Descripcion = a.Descripcion,
                Monto = a.Monto,
                CategoriaId = a.CategoriaId,
                CategoriaNombre = a.Categoria == null ? null : a.Categoria.Nombre,
                CuentaId = a.CuentaId,
                CuentaNombre = a.Cuenta.Nombre,
                Frecuencia = a.Frecuencia,
                FechaInicio = a.FechaInicio,
                ProximaFecha = a.ProximaFecha,
                FechaFin = a.FechaFin,
                Activa = a.Activa
            })
            .ToListAsync();

        return new BaseResponse<List<RecurrenteModel>> { Success = true, Data = lista };
    }

    // Una fila por recurrente con la ocurrencia vencida más antigua; `Atrasadas` cuenta las que siguen.
    public async Task<BaseResponse<List<RecurrentePendienteModel>>> GetPendientes(int idUsuario)
    {
        var hoy = DateTime.Today;
        var vencidas = await context.Recurrentes.AsNoTracking()
            .Where(a => a.UsuarioId == idUsuario && a.Activa && a.ProximaFecha <= hoy
                        && (a.FechaFin == null || a.ProximaFecha <= a.FechaFin))
            .OrderBy(a => a.ProximaFecha).ThenBy(a => a.Descripcion)
            .Select(a => new
            {
                a.Id, a.TipoOperacion, a.Descripcion, a.Monto, a.Frecuencia, a.FechaInicio, a.ProximaFecha, a.FechaFin,
                Categoria = a.Categoria == null ? null : a.Categoria.Nombre,
                Cuenta = a.Cuenta.Nombre
            })
            .ToListAsync();

        var lista = vencidas.Select(a =>
        {
            var atrasadas = 0;
            var fecha = FrecuenciaRecurrente.Siguiente(a.ProximaFecha, a.FechaInicio, a.Frecuencia);
            while (fecha <= hoy && (a.FechaFin == null || fecha <= a.FechaFin) && atrasadas < MaxAtrasadas)
            {
                atrasadas++;
                fecha = FrecuenciaRecurrente.Siguiente(fecha, a.FechaInicio, a.Frecuencia);
            }

            return new RecurrentePendienteModel
            {
                RecurrenteId = a.Id,
                TipoOperacion = a.TipoOperacion,
                Descripcion = a.Descripcion,
                Monto = a.Monto,
                CategoriaNombre = a.Categoria,
                CuentaNombre = a.Cuenta,
                Fecha = a.ProximaFecha,
                Atrasadas = atrasadas
            };
        }).ToList();

        return new BaseResponse<List<RecurrentePendienteModel>> { Success = true, Data = lista };
    }

    public async Task<BaseResponse> Save(int idUsuario, RecurrenteModel modelo)
    {
        var check = await Validar(idUsuario, modelo);
        if (!check.Success) return check;

        var nueva = new Recurrente
        {
            UsuarioId = idUsuario,
            TipoOperacion = modelo.TipoOperacion,
            Descripcion = modelo.Descripcion.Trim(),
            Monto = decimal.Round(modelo.Monto, 2),
            CategoriaId = modelo.CategoriaId,
            CuentaId = modelo.CuentaId,
            Frecuencia = modelo.Frecuencia,
            FechaInicio = modelo.FechaInicio.Date,
            ProximaFecha = modelo.FechaInicio.Date,
            FechaFin = modelo.FechaFin?.Date,
            Activa = true
        };

        context.Recurrentes.Add(nueva);
        await context.SaveChangesAsync();
        return new BaseResponse { Success = true, Message = "Guardado", Id = nueva.Id };
    }

    public async Task<BaseResponse> Edit(int idUsuario, RecurrenteModel modelo)
    {
        var check = await Validar(idUsuario, modelo);
        if (!check.Success) return check;

        var actual = await context.Recurrentes.FirstOrDefaultAsync(a => a.Id == modelo.Id && a.UsuarioId == idUsuario);
        if (actual == null) return new BaseResponse { Message = "La recurrente no existe" };

        actual.TipoOperacion = modelo.TipoOperacion;
        actual.Descripcion = modelo.Descripcion.Trim();
        actual.Monto = decimal.Round(modelo.Monto, 2);
        actual.CategoriaId = modelo.CategoriaId;
        actual.CuentaId = modelo.CuentaId;
        actual.Frecuencia = modelo.Frecuencia;
        actual.FechaInicio = modelo.FechaInicio.Date;
        actual.ProximaFecha = modelo.ProximaFecha.Date;
        actual.FechaFin = modelo.FechaFin?.Date;
        actual.Activa = modelo.Activa;

        await context.SaveChangesAsync();
        return new BaseResponse { Success = true, Message = "Guardado", Id = actual.Id };
    }

    public async Task<BaseResponse> Delete(int idUsuario, int idRecurrente)
    {
        var actual = await context.Recurrentes.FirstOrDefaultAsync(a => a.Id == idRecurrente && a.UsuarioId == idUsuario);
        if (actual == null) return new BaseResponse { Message = "La recurrente no existe" };

        // Las operaciones ya confirmadas son independientes y se conservan.
        context.Recurrentes.Remove(actual);
        await context.SaveChangesAsync();
        return new BaseResponse { Success = true, Message = "Eliminada" };
    }

    public async Task<BaseResponse> Confirmar(int idUsuario, ConfirmarRecurrenteRequest request)
    {
        var recurrente = await context.Recurrentes.FirstOrDefaultAsync(a => a.Id == request.RecurrenteId && a.UsuarioId == idUsuario);
        if (recurrente == null) return new BaseResponse { Message = "La recurrente no existe" };

        var vigente = Vigente(recurrente, request.Fecha);
        if (!vigente.Success) return vigente;

        var monto = decimal.Round(request.Monto ?? recurrente.Monto, 2);
        if (monto <= 0) return new BaseResponse { Message = "El monto debe ser mayor a cero" };

        var esGasto = recurrente.TipoOperacion == TipoOperacion.Gasto;
        var operacion = new OperacionRequest
        {
            TipoOperacion = recurrente.TipoOperacion,
            Monto = monto,
            Fecha = recurrente.ProximaFecha.Date,
            Descripcion = recurrente.Descripcion,
            CategoriaId = recurrente.CategoriaId,
            CuentaOrigenId = esGasto ? recurrente.CuentaId : null,
            CuentaDestinoId = esGasto ? null : recurrente.CuentaId
        };

        // Se avanza la fecha antes de guardar la operación: OperacionService.Save hace un solo SaveChanges con el mismo
        // contexto, así la operación y el avance se guardan juntos (o ninguno) y no se duplica al reintentar.
        var fechaOriginal = recurrente.ProximaFecha;
        recurrente.ProximaFecha = FrecuenciaRecurrente.Siguiente(fechaOriginal, recurrente.FechaInicio, recurrente.Frecuencia);

        var resultado = await operaciones.Save(idUsuario, operacion);
        if (!resultado.Success)
        {
            context.Entry(recurrente).State = EntityState.Detached;
            return resultado;
        }

        return new BaseResponse { Success = true, Message = "Confirmada", Id = resultado.Id };
    }

    public async Task<BaseResponse> Omitir(int idUsuario, OmitirRecurrenteRequest request)
    {
        var recurrente = await context.Recurrentes.FirstOrDefaultAsync(a => a.Id == request.RecurrenteId && a.UsuarioId == idUsuario);
        if (recurrente == null) return new BaseResponse { Message = "La recurrente no existe" };

        var vigente = Vigente(recurrente, request.Fecha);
        if (!vigente.Success) return vigente;

        recurrente.ProximaFecha = FrecuenciaRecurrente.Siguiente(recurrente.ProximaFecha, recurrente.FechaInicio, recurrente.Frecuencia);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            context.ChangeTracker.Clear();
            return new BaseResponse { Message = "Esa ocurrencia ya se atendió en otra pantalla; recarga" };
        }
        return new BaseResponse { Success = true, Message = "Omitida" };
    }

    // ---- Apoyo ----

    // La ocurrencia que se atiende debe ser exactamente la próxima pendiente y ya haber llegado.
    private static BaseResponse Vigente(Recurrente r, DateTime fecha)
    {
        if (!r.Activa) return new BaseResponse { Message = "La recurrente está inactiva" };
        if (fecha.Date != r.ProximaFecha.Date) return new BaseResponse { Message = "Esa ocurrencia ya no está pendiente; recarga la pantalla" };
        if (r.ProximaFecha.Date > DateTime.Today) return new BaseResponse { Message = "Todavía no llega la fecha de esta ocurrencia" };
        if (r.FechaFin != null && r.ProximaFecha.Date > r.FechaFin.Value.Date) return new BaseResponse { Message = "La recurrente ya terminó" };
        return new BaseResponse { Success = true };
    }

    private async Task<BaseResponse> Validar(int idUsuario, RecurrenteModel m)
    {
        if (string.IsNullOrWhiteSpace(m.Descripcion)) return new BaseResponse { Message = "Ingresa una descripción" };
        if (m.Descripcion.Trim().Length > 250) return new BaseResponse { Message = "Descripción supera los caracteres permitidos" };
        if (m.TipoOperacion is not (TipoOperacion.Gasto or TipoOperacion.Ingreso))
            return new BaseResponse { Message = "Solo se pueden programar gastos e ingresos" };
        if (decimal.Round(m.Monto, 2) <= 0) return new BaseResponse { Message = "El monto debe ser mayor a cero" };
        if (!FrecuenciaRecurrente.EsValida(m.Frecuencia)) return new BaseResponse { Message = "Frecuencia inválida" };
        if (m.FechaFin != null && m.FechaFin.Value.Date < m.FechaInicio.Date) return new BaseResponse { Message = "La fecha final no puede ser anterior al inicio" };

        var cuenta = await context.Cuentas.AsNoTracking().FirstOrDefaultAsync(a => a.Id == m.CuentaId && a.UsuarioId == idUsuario);
        if (cuenta == null) return new BaseResponse { Message = "Cuenta no encontrada" };
        if (!cuenta.Activa) return new BaseResponse { Message = $"La cuenta {cuenta.Nombre} está inactiva" };
        if (m.TipoOperacion == TipoOperacion.Ingreso && cuenta.TipoCuenta == TipoCuentaItem.Credito)
            return new BaseResponse { Message = "Un ingreso no puede abonarse a una tarjeta de crédito" };

        if (m.CategoriaId != null && !await context.Categorias.AnyAsync(a => a.Id == m.CategoriaId && a.Activa && (a.UsuarioId == null || a.UsuarioId == idUsuario)))
            return new BaseResponse { Message = "Categoría no encontrada" };

        return new BaseResponse { Success = true };
    }
}
