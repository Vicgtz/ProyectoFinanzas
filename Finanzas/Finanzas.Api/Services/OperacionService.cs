using Finanzas.Data;
using Finanzas.Models;
using Finanzas.Models.Cuenta;
using Finanzas.Models.Operacion;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Api.Services;

// Cada operación genera movimientos en MovimientoCuenta cuyo Importe es el cambio aplicado a Cuenta.Saldo.
// En cuentas de crédito el saldo es la deuda: un gasto lo sube y un pago lo baja.
public class OperacionService(FinanzasDbContext context)
{
    private static readonly string[] TiposSoportados =
        [TipoOperacion.Gasto, TipoOperacion.Ingreso, TipoOperacion.Transferencia, TipoOperacion.PagoTarjeta,
         TipoOperacion.PrestamoOtorgado, TipoOperacion.DeudaAdquirida, TipoOperacion.CobroPrestamo, TipoOperacion.PagoDeudaPersona];

    // Préstamos y deudas ligados a una persona: no son ingresos ni gastos.
    private static readonly string[] TiposPersona =
        [TipoOperacion.PrestamoOtorgado, TipoOperacion.DeudaAdquirida, TipoOperacion.CobroPrestamo, TipoOperacion.PagoDeudaPersona];

    public async Task<BaseResponse<OperacionModel>> Get(int idUsuario, long idOperacion)
    {
        var operacion = await Consulta(idUsuario).Where(a => a.Id == idOperacion).FirstOrDefaultAsync();
        if (operacion == null) return new BaseResponse<OperacionModel> { Message = "Operación no encontrada" };
        return new BaseResponse<OperacionModel> { Success = true, Data = operacion };
    }

    public async Task<BaseResponse<List<OperacionModel>>> GetList(int idUsuario, OperacionFiltro filtro)
    {
        var query = context.Operacions.AsNoTracking().Where(a => a.UsuarioId == idUsuario);

        if (filtro.Desde != null) query = query.Where(a => a.Fecha >= filtro.Desde);
        if (filtro.Hasta != null) query = query.Where(a => a.Fecha < filtro.Hasta.Value.Date.AddDays(1));
        if (!string.IsNullOrEmpty(filtro.TipoOperacion)) query = query.Where(a => a.TipoOperacion == filtro.TipoOperacion);
        if (filtro.CategoriaId != null) query = query.Where(a => a.CategoriaId == filtro.CategoriaId);
        if (filtro.PersonaId != null) query = query.Where(a => a.PersonaId == filtro.PersonaId);
        if (filtro.CuentaId != null) query = query.Where(a => a.MovimientoCuentas.Any(m => m.CuentaId == filtro.CuentaId));

        var take = Math.Clamp(filtro.Take, 1, 200);
        var lista = await query
            .OrderByDescending(a => a.Fecha).ThenByDescending(a => a.Id)
            .Skip(Math.Max(filtro.Skip, 0)).Take(take)
            .Select(Proyeccion)
            .ToListAsync();

        return new BaseResponse<List<OperacionModel>> { Success = true, Data = lista };
    }

    public async Task<BaseResponse> Save(int idUsuario, OperacionRequest request)
    {
        var check = ValidarRequest(request);
        if (!check.Success) return check;

        var categoria = await ValidarCategoria(request);
        if (!categoria.Success) return categoria;

        var cuentas = await CargarCuentas(idUsuario, request.CuentaOrigenId, request.CuentaDestinoId);

        var operacion = new Operacion { UsuarioId = idUsuario };
        var aplicado = Aplicar(operacion, request, cuentas);
        if (aplicado.Success) aplicado = AplicarMsi(operacion, request, cuentas);
        if (!aplicado.Success) { context.ChangeTracker.Clear(); return aplicado; }
        var saldos = ValidarSaldos(cuentas);
        if (!saldos.Success) { context.ChangeTracker.Clear(); return saldos; }

        var persona = await ValidarPersona(idUsuario, request, null, null);
        if (!persona.Success) { context.ChangeTracker.Clear(); return persona; }

        context.Operacions.Add(operacion);
        return await Guardar(operacion);
    }

    public async Task<BaseResponse> Edit(int idUsuario, OperacionRequest request)
    {
        var check = ValidarRequest(request);
        if (!check.Success) return check;

        var categoria = await ValidarCategoria(request);
        if (!categoria.Success) return categoria;

        var operacion = await context.Operacions.Include(a => a.MovimientoCuentas).Include(a => a.CompraMsi)
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.UsuarioId == idUsuario);
        if (operacion == null) return new BaseResponse { Message = "Operación no existe" };
        if (!TiposSoportados.Contains(operacion.TipoOperacion))
            return new BaseResponse { Message = "Este tipo de operación aún no se puede editar" };

        var ids = operacion.MovimientoCuentas.Select(m => (int?)m.CuentaId)
            .Concat([request.CuentaOrigenId, request.CuentaDestinoId]).ToArray();
        var cuentas = await CargarCuentas(idUsuario, ids);

        var reversa = Revertir(operacion, cuentas);
        if (!reversa.Success) { context.ChangeTracker.Clear(); return reversa; }

        var personaAnterior = operacion.PersonaId;
        context.MovimientoCuentas.RemoveRange(operacion.MovimientoCuentas.ToList());
        operacion.MovimientoCuentas.Clear();

        var aplicado = Aplicar(operacion, request, cuentas);
        if (aplicado.Success) aplicado = AplicarMsi(operacion, request, cuentas);
        if (!aplicado.Success) { context.ChangeTracker.Clear(); return aplicado; }
        var saldos = ValidarSaldos(cuentas);
        if (!saldos.Success) { context.ChangeTracker.Clear(); return saldos; }

        var persona = await ValidarPersona(idUsuario, request, operacion.Id, personaAnterior);
        if (!persona.Success) { context.ChangeTracker.Clear(); return persona; }

        operacion.FechaActualizacion = DateTime.UtcNow;
        return await Guardar(operacion);
    }

    public async Task<BaseResponse> Delete(int idUsuario, long idOperacion)
    {
        var operacion = await context.Operacions.Include(a => a.MovimientoCuentas).Include(a => a.CompraMsi)
            .FirstOrDefaultAsync(a => a.Id == idOperacion && a.UsuarioId == idUsuario);
        if (operacion == null) return new BaseResponse { Message = "Operación no existe" };
        if (!TiposSoportados.Contains(operacion.TipoOperacion))
            return new BaseResponse { Message = "Este tipo de operación aún no se puede eliminar" };

        var cuentas = await CargarCuentas(idUsuario, operacion.MovimientoCuentas.Select(m => (int?)m.CuentaId).ToArray());
        var reversa = Revertir(operacion, cuentas);
        if (!reversa.Success) { context.ChangeTracker.Clear(); return reversa; }
        var saldos = ValidarSaldos(cuentas);
        if (!saldos.Success) { context.ChangeTracker.Clear(); return saldos; }

        // No se puede borrar un préstamo o deuda que ya tiene abonos: el saldo de la persona quedaría negativo.
        var persona = await ValidarPersona(idUsuario, null, operacion.Id, operacion.PersonaId);
        if (!persona.Success) { context.ChangeTracker.Clear(); return persona; }

        context.MovimientoCuentas.RemoveRange(operacion.MovimientoCuentas);
        if (operacion.CompraMsi != null) context.CompraMsis.Remove(operacion.CompraMsi);
        context.Operacions.Remove(operacion);
        return await Guardar(operacion);
    }

    // ---- Lógica de saldos ----

    private static BaseResponse Aplicar(Operacion operacion, OperacionRequest r, Dictionary<int, Cuenta> cuentas)
    {
        var monto = decimal.Round(r.Monto, 2);
        var movimientos = new List<(Cuenta Cuenta, decimal Importe)>();

        Cuenta? Buscar(int? id, string rol, out BaseResponse? error)
        {
            error = null;
            if (id == null || !cuentas.TryGetValue(id.Value, out var c))
            {
                error = new BaseResponse { Message = $"Cuenta {rol} no encontrada" };
                return null;
            }
            if (!c.Activa) error = new BaseResponse { Message = $"La cuenta {c.Nombre} está inactiva" };
            return c;
        }

        BaseResponse? err;
        switch (r.TipoOperacion)
        {
            case TipoOperacion.Gasto:
            {
                var origen = Buscar(r.CuentaOrigenId, "origen", out err);
                if (err != null) return err;
                movimientos.Add((origen!, EsCredito(origen!) ? monto : -monto));
                break;
            }
            case TipoOperacion.Ingreso:
            {
                var destino = Buscar(r.CuentaDestinoId, "destino", out err);
                if (err != null) return err;
                if (EsCredito(destino!)) return new BaseResponse { Message = "Un ingreso no puede abonarse a una tarjeta de crédito; usa PagoTarjeta" };
                movimientos.Add((destino!, monto));
                break;
            }
            case TipoOperacion.Transferencia:
            {
                var origen = Buscar(r.CuentaOrigenId, "origen", out err);
                if (err != null) return err;
                var destino = Buscar(r.CuentaDestinoId, "destino", out err);
                if (err != null) return err;
                if (origen!.Id == destino!.Id) return new BaseResponse { Message = "El origen y el destino deben ser distintos" };
                if (EsCredito(origen) || EsCredito(destino)) return new BaseResponse { Message = "Las transferencias solo aplican entre cuentas de débito o efectivo" };
                movimientos.Add((origen, -monto));
                movimientos.Add((destino, monto));
                break;
            }
            case TipoOperacion.PagoTarjeta:
            {
                var origen = Buscar(r.CuentaOrigenId, "origen", out err);
                if (err != null) return err;
                var tarjeta = Buscar(r.CuentaDestinoId, "destino", out err);
                if (err != null) return err;
                if (EsCredito(origen!)) return new BaseResponse { Message = "El pago debe salir de una cuenta de débito o efectivo" };
                if (!EsCredito(tarjeta!)) return new BaseResponse { Message = "La cuenta destino debe ser una tarjeta de crédito" };
                movimientos.Add((origen!, -monto));
                movimientos.Add((tarjeta!, -monto));
                break;
            }
            // Préstamos y deudas personales: solo mueven efectivo o débito, nunca una tarjeta.
            case TipoOperacion.PrestamoOtorgado or TipoOperacion.DeudaAdquirida when !r.AfectaCuenta:
                break;
            case TipoOperacion.PrestamoOtorgado:
            case TipoOperacion.PagoDeudaPersona:
            {
                var origen = Buscar(r.CuentaOrigenId, "origen", out err);
                if (err != null) return err;
                if (EsCredito(origen!)) return new BaseResponse { Message = "Esta operación debe salir de una cuenta de débito o efectivo" };
                movimientos.Add((origen!, -monto));
                break;
            }
            case TipoOperacion.DeudaAdquirida:
            case TipoOperacion.CobroPrestamo:
            {
                var destino = Buscar(r.CuentaDestinoId, "destino", out err);
                if (err != null) return err;
                if (EsCredito(destino!)) return new BaseResponse { Message = "El dinero debe entrar a una cuenta de débito o efectivo" };
                movimientos.Add((destino!, monto));
                break;
            }
            default:
                return new BaseResponse { Message = "Tipo de operación no soportado" };
        }

        foreach (var (cuenta, importe) in movimientos)
        {
            cuenta.Saldo += importe;
            operacion.MovimientoCuentas.Add(new MovimientoCuenta { CuentaId = cuenta.Id, Importe = importe });
        }

        operacion.TipoOperacion = r.TipoOperacion;
        operacion.PersonaId = TiposPersona.Contains(r.TipoOperacion) ? r.PersonaId : null;
        operacion.Monto = monto;
        operacion.Fecha = r.Fecha ?? DateTime.Now;
        operacion.Descripcion = string.IsNullOrWhiteSpace(r.Descripcion) ? null : r.Descripcion.Trim();
        operacion.CategoriaId = r.TipoOperacion is TipoOperacion.Gasto or TipoOperacion.Ingreso ? r.CategoriaId : null;
        return new BaseResponse { Success = true };
    }

    // Deshace los movimientos de una operación sobre los saldos y valida que sigan siendo válidos.
    private static BaseResponse Revertir(Operacion operacion, Dictionary<int, Cuenta> cuentas)
    {
        foreach (var mov in operacion.MovimientoCuentas.ToList())
        {
            if (!cuentas.TryGetValue(mov.CuentaId, out var cuenta))
                return new BaseResponse { Message = "No se encontró una cuenta de la operación" };
            cuenta.Saldo -= mov.Importe;
        }
        return new BaseResponse { Success = true };
    }

    private static BaseResponse ValidarSaldos(Dictionary<int, Cuenta> cuentas)
    {
        foreach (var cuenta in cuentas.Values)
        {
            var r = ValidarSaldo(cuenta);
            if (!r.Success) return r;
        }
        return new BaseResponse { Success = true };
    }

    private static BaseResponse ValidarSaldo(Cuenta c)
    {
        if (EsCredito(c))
        {
            if (c.Saldo < 0) return new BaseResponse { Message = $"El pago excede la deuda de {c.Nombre}" };
            if (c.LimiteCredito != null && c.Saldo > c.LimiteCredito) return new BaseResponse { Message = $"Se excede el límite de crédito de {c.Nombre}" };
        }
        else if (c.Saldo < 0)
        {
            return new BaseResponse { Message = $"Saldo insuficiente en {c.Nombre}" };
        }
        return new BaseResponse { Success = true };
    }

    private static bool EsCredito(Cuenta c) => c.TipoCuenta == TipoCuentaItem.Credito;

    // Saldo con una persona: por cobrar = prestado - abonado por ella; por pagar = recibido - pagado por mí.
    // Se revalida con las operaciones ya guardadas (sin la que se edita o borra) más la nueva, para que ninguno quede negativo.
    private async Task<BaseResponse> ValidarPersona(int idUsuario, OperacionRequest? nueva, long? excluirId, int? personaAnterior)
    {
        var aplicaNueva = nueva != null && TiposPersona.Contains(nueva.TipoOperacion);
        var ids = new HashSet<int>();
        if (aplicaNueva) ids.Add(nueva!.PersonaId!.Value);
        if (personaAnterior != null) ids.Add(personaAnterior.Value);

        if (aplicaNueva)
        {
            var persona = await context.Personas.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == nueva!.PersonaId && a.UsuarioId == idUsuario);
            if (persona == null) return new BaseResponse { Message = "Persona no encontrada" };
            if (!persona.Activa && nueva!.PersonaId != personaAnterior) return new BaseResponse { Message = $"La persona {persona.Nombre} está inactiva" };
        }

        foreach (var id in ids)
        {
            var totales = await context.Operacions.AsNoTracking()
                .Where(a => a.UsuarioId == idUsuario && a.PersonaId == id && a.Id != excluirId)
                .GroupBy(a => a.TipoOperacion)
                .Select(g => new { Tipo = g.Key, Total = g.Sum(x => x.Monto) })
                .ToDictionaryAsync(g => g.Tipo, g => g.Total);

            decimal Total(string tipo) => totales.GetValueOrDefault(tipo) +
                (aplicaNueva && nueva!.PersonaId == id && nueva.TipoOperacion == tipo ? decimal.Round(nueva.Monto, 2) : 0);

            var borrando = nueva == null;
            if (Total(TipoOperacion.PrestamoOtorgado) - Total(TipoOperacion.CobroPrestamo) < 0)
                return new BaseResponse { Message = borrando ? "No se puede eliminar: el préstamo ya tiene abonos registrados" : "El abono recibido no puede superar lo que esta persona te debe" };
            if (Total(TipoOperacion.DeudaAdquirida) - Total(TipoOperacion.PagoDeudaPersona) < 0)
                return new BaseResponse { Message = borrando ? "No se puede eliminar: la deuda ya tiene pagos registrados" : "El pago no puede superar lo que le debes a esta persona" };
        }
        return new BaseResponse { Success = true };
    }

    // Compra a meses sin intereses: el gasto ya se registró completo en Aplicar (sube la deuda por el total);
    // aquí solo se guarda el plan de mensualidades, sin crear gastos ficticios.
    private BaseResponse AplicarMsi(Operacion operacion, OperacionRequest r, Dictionary<int, Cuenta> cuentas)
    {
        var existente = operacion.CompraMsi;
        if (r.NumeroMeses == null)
        {
            if (existente != null)
            {
                context.CompraMsis.Remove(existente);
                operacion.CompraMsi = null;
            }
            return new BaseResponse { Success = true };
        }

        var tarjeta = cuentas[r.CuentaOrigenId!.Value];
        if (!EsCredito(tarjeta)) return new BaseResponse { Message = "Los meses sin intereses solo aplican a tarjetas de crédito" };

        var meses = r.NumeroMeses.Value;
        var mensual = decimal.Round(operacion.Monto / meses, 2);
        if (mensual <= 0 || operacion.Monto - mensual * (meses - 1) <= 0)
            return new BaseResponse { Message = "El monto es demasiado pequeño para ese número de meses" };

        var primera = PrimerCorteDespues(operacion.Fecha.Date, tarjeta.DiaCorte ?? 1);
        var msi = existente ?? new CompraMsi();
        msi.CuentaCreditoId = tarjeta.Id;
        msi.MontoCompra = operacion.Monto;
        msi.NumeroMeses = (short)meses;
        msi.ImporteMensual = mensual;
        msi.FechaPrimeraMensualidad = primera;
        operacion.CompraMsi = msi;
        return new BaseResponse { Success = true };
    }

    // Primer corte posterior a la compra: la compra hecha el día de corte cae en el periodo siguiente.
    private static DateTime PrimerCorteDespues(DateTime fecha, int diaCorte)
    {
        var esteMes = new DateTime(fecha.Year, fecha.Month, Math.Min(diaCorte, DateTime.DaysInMonth(fecha.Year, fecha.Month)));
        if (esteMes > fecha) return esteMes;
        var siguiente = fecha.AddMonths(1);
        return new DateTime(siguiente.Year, siguiente.Month, Math.Min(diaCorte, DateTime.DaysInMonth(siguiente.Year, siguiente.Month)));
    }

    // ---- Apoyo ----

    private async Task<Dictionary<int, Cuenta>> CargarCuentas(int idUsuario, params int?[] ids)
    {
        var lista = ids.Where(i => i != null).Select(i => i!.Value).Distinct().ToList();
        return await context.Cuentas.Where(a => a.UsuarioId == idUsuario && lista.Contains(a.Id)).ToDictionaryAsync(a => a.Id);
    }

    private async Task<BaseResponse> Guardar(Operacion operacion)
    {
        try
        {
            await context.SaveChangesAsync();
            return new BaseResponse { Success = true, Message = "Guardado", Id = operacion.Id };
        }
        catch (DbUpdateConcurrencyException)
        {
            context.ChangeTracker.Clear();
            return new BaseResponse { Message = "Los saldos cambiaron mientras se guardaba; intenta de nuevo" };
        }
    }

    private async Task<BaseResponse> ValidarCategoria(OperacionRequest r)
    {
        if (r.CategoriaId == null) return new BaseResponse { Success = true };
        var existe = await context.Categorias.AnyAsync(a => a.Id == r.CategoriaId && a.Activa);
        return existe ? new BaseResponse { Success = true } : new BaseResponse { Message = "Categoría no encontrada" };
    }

    private static BaseResponse ValidarRequest(OperacionRequest r)
    {
        if (!TiposSoportados.Contains(r.TipoOperacion)) return new BaseResponse { Message = "Tipo de operación inválido" };
        if (r.Monto <= 0) return new BaseResponse { Message = "El monto debe ser mayor a cero" };
        if (TiposPersona.Contains(r.TipoOperacion) && r.PersonaId == null) return new BaseResponse { Message = "Selecciona la persona" };
        if (r.NumeroMeses != null && r.TipoOperacion != TipoOperacion.Gasto) return new BaseResponse { Message = "Los meses sin intereses solo aplican a gastos" };
        if (r.NumeroMeses is < 2 or > 60) return new BaseResponse { Message = "Los meses sin intereses deben estar entre 2 y 60" };
        if (decimal.Round(r.Monto, 2) <= 0) return new BaseResponse { Message = "El monto es demasiado pequeño" };
        if (r.Fecha != null && r.Fecha.Value.Date > DateTime.Today) return new BaseResponse { Message = "No se permiten operaciones con fecha futura" };
        if (r.Descripcion?.Trim().Length > 250) return new BaseResponse { Message = "Descripción supera los caracteres permitidos" };
        return new BaseResponse { Success = true };
    }

    private IQueryable<OperacionModel> Consulta(int idUsuario) =>
        context.Operacions.AsNoTracking().Where(a => a.UsuarioId == idUsuario).Select(Proyeccion);

    private static readonly System.Linq.Expressions.Expression<Func<Operacion, OperacionModel>> Proyeccion = a => new OperacionModel
    {
        Id = a.Id,
        TipoOperacion = a.TipoOperacion,
        Monto = a.Monto,
        Fecha = a.Fecha,
        Descripcion = a.Descripcion,
        CategoriaId = a.CategoriaId,
        CategoriaNombre = a.Categoria == null ? null : a.Categoria.Nombre,
        PersonaId = a.PersonaId,
        PersonaNombre = a.Persona == null ? null : a.Persona.Nombre,
        Msi = a.CompraMsi == null ? null : new MsiOperacionModel
        {
            NumeroMeses = a.CompraMsi.NumeroMeses,
            ImporteMensual = a.CompraMsi.ImporteMensual,
            FechaPrimeraMensualidad = a.CompraMsi.FechaPrimeraMensualidad
        },
        Movimientos = a.MovimientoCuentas.Select(m => new MovimientoModel
        {
            CuentaId = m.CuentaId,
            CuentaNombre = m.Cuenta.Nombre,
            Importe = m.Importe
        }).ToList()
    };
}
