# Finanzas

Aplicación de finanzas personales en español (pesos mexicanos): ingresos, gastos, cuentas, tarjetas de crédito con corte y meses sin intereses (MSI), préstamos entre personas y análisis. Una API en .NET 9 con SQL Server y un cliente Blazor WebAssembly.

## Estructura

La solución es `Finanzas/Finanzas.sln` (el repositorio git está un nivel arriba, en `ProyectoFinanzas`).

| Proyecto | Qué contiene |
|---|---|
| `Finanzas/Finanzas.Api` | ASP.NET Core Web API: `Controllers/` (delgados) y `Services/` (toda la lógica), JWT, CORS, `appsettings*.json` |
| `Finanzas/Finanzas.Data` | EF Core 9 + SQL Server: `Entities/`, `Configuration/`, `FinanzasDbContext`. Es código generado (EF Reverse POCO, plantilla `DatabaseFinanzas.tt`): no editar a mano |
| `Finanzas/Finanzas.Models` | DTOs compartidos entre API y cliente (`BaseResponse`, modelos por área, constantes como `TipoOperacion`) |
| `Finanzas/Finanzas` | Cliente Blazor WASM: `Pages/`, `Components/`, `Layout/`, `Services/` (clientes HTTP), `wwwroot/` |

La base de datos ya existe (SQL Server local, `FinanzasDB`, autenticación integrada). No hay migraciones: el esquema se crea fuera del repositorio y las entidades se generan desde él.

`Scripts/` guarda los scripts SQL de los cambios de esquema, numerados (`01_DiasParaPago.sql`, `02_Recurrente.sql`, `03_Presupuesto.sql`, `04_CategoriaUsuario.sql`); son idempotentes y se ejecutan a mano en SSMS **antes** de iniciar la API que los usa. Las entidades `Recurrente` y `Presupuesto`, las columnas `Cuenta.DiasParaPago` y `Categoria.UsuarioId` y sus configuraciones se escribieron a mano con el formato del generador: al volver a correr la plantilla `.tt` con esas tablas ya creadas deben quedar equivalentes.

## Ejecutar

- API: `dotnet run --project Finanzas/Finanzas.Api --launch-profile http` → `http://localhost:5137`
- Web: `dotnet run --project Finanzas/Finanzas --launch-profile http` → `http://localhost:5150` (la URL de la API está en `Finanzas/Finanzas/wwwroot/appsettings.json`; `appsettings.Development.json` apunta a localhost al correr en local y `appsettings.Production.json` al servidor `192.168.68.124:5000` al publicar)
- Compilar: `dotnet build Finanzas/Finanzas.sln`
- Las funciones de fecha límite de pago, recurrentes y presupuestos se escribieron sin poder compilar (sin acceso a NuGet): si `dotnet build` marca algo, corregirlo primero.
- `.claude/launch.json` define las configuraciones `api` y `web` para la vista previa de la herramienta.
- Cuidado: no compilar la solución mientras la web o la API están corriendo; los archivos quedan bloqueados o la web sirve archivos obsoletos. Detener, compilar y volver a iniciar.
- El CORS de la API solo admite `http://localhost:5150` y `https://localhost:7132`.
- No hay pruebas automatizadas; se verifica con la API y la interfaz.
- Usuario de prueba solo para desarrollo local: ver `Finanzas/Finanzas.Api/dev-user.txt`. Ese archivo no está en `.gitignore`: no poner ahí contraseñas reales.

## Autenticación

Login y registro en `api/Usuario/Login` y `api/Usuario/Registrar` (contraseña con hash propio, `PasswordHasher`). El registro (página `/registro`) exige la clave `Registro:Clave` de la configuración de la API; si está vacía el registro queda deshabilitado. Definirla fuera del repositorio: variable de entorno `Registro__Clave` (en local y en el servidor), o `dotnet user-secrets init` + `dotnet user-secrets set "Registro:Clave" "..."` desde `Finanzas/Finanzas.Api`. El token JWT trae el claim `IdUsuario`; los controladores lo leen con la propiedad `IdUsuario` y **todo servicio filtra por usuario**. El resto de controladores usan `[Authorize]`. En el cliente, `CustomAuthProvider` y `AuthHttpHandler` guardan el token en localStorage y lo envían en cada llamada.

## Convenciones del código

- **Servicios con primary constructor** (`class XService(FinanzasDbContext context)`); controladores sin lógica que devuelven `Ok(await servicio.Metodo(IdUsuario, ...))`.
- **Respuestas**: siempre `BaseResponse` / `BaseResponse<T>` con `Success`, `Message` y `Data` (o `Id`). Los errores de negocio se devuelven como `Success = false` con un mensaje en español, no como excepciones ni códigos HTTP.
- **Cliente**: cada área tiene un servicio que hereda de `HttpService` (`GetData<T>`, `PostResponse`); se registran en `Program.cs`.
- **Interfaz**: páginas `.razor` con code-behind `.razor.cs` (clase `partial`); modales hechos a mano con clases de Bootstrap; formulario de movimientos reutilizable en `Components/MovimientoForm`. Estilos globales en `wwwroot/css/app.css` (variables `--fz-*`, tema verde) y CSS con alcance por página (`*.razor.css`).
- **Formato**: usar `Finanzas.Services.Formato.Moneda` para importes (es-MX). En estilos inline, formatear números con `CultureInfo.InvariantCulture` (la cultura es-MX usa coma decimal y rompe el CSS).
- **Gráficas**: SVG/CSS propio, sin librerías.
- En Razor, un parámetro `string` con comillas simples se toma como texto literal: usar `Param="@variable"`.
- Textos de la interfaz y mensajes de error en español.

## Modelo financiero

Tablas principales: `Usuario`, `Cuenta`, `Categoria`, `Persona`, `Operacion`, `MovimientoCuenta`, `CompraMSI`, `CorteTarjeta`, `SesionUsuario`, `Recurrente`, `Presupuesto`.

- **Cuentas** (`TipoCuenta`): `E` efectivo, `D` débito, `C` crédito. En crédito, `Saldo` es la **deuda utilizada**; el crédito disponible es `LimiteCredito - Saldo` y no es dinero propio. Solo las de crédito tienen límite y día de corte (1 a 31; con 31 cae el último día de meses cortos). Los saldos solo cambian mediante operaciones; nunca se editan directo.
- **Operaciones** (`TipoOperacion`, restringido por un CHECK en la base): `Gasto`, `Ingreso`, `Transferencia`, `PagoTarjeta`, `PrestamoOtorgado`, `DeudaAdquirida`, `CobroPrestamo`, `PagoDeudaPersona`. Cada una genera filas en `MovimientoCuenta` cuyo `Importe` es el cambio aplicado a `Cuenta.Saldo`; editar o eliminar revierte esos movimientos y vuelve a aplicar. Un tipo nuevo exige actualizar el CHECK de la base, `TiposSoportados` y `Aplicar` en `OperacionService`.
- **Reglas de saldo**: ninguna cuenta de débito o efectivo queda en negativo; una tarjeta no puede pasar su límite ni quedar con deuda negativa. Concurrencia con `DbUpdateConcurrencyException`.
- **Fechas**: las operaciones pueden tener fecha pasada, nunca futura.
- **Ingreso y gasto** son lo único que cuenta como ingresos/gastos en dashboard y análisis. Transferencias, pagos de tarjeta y préstamos no cuentan; el pago de tarjeta no duplica el gasto original.
- **Transferencias** y **pagos de tarjeta** solo salen de débito o efectivo; el destino del pago es una tarjeta.
- **Cortes de tarjeta**: la compra hecha el día de corte pertenece al periodo siguiente (un periodo es `[corte anterior, corte siguiente)`). `CuentaDetalleService` calcula último y próximo corte y registra la deuda al corte, excluyendo lo fechado desde ese día. Hay un corte único por cuenta y fecha, y no puede ser futuro.
- **MSI**: es un `Gasto` en tarjeta con un registro `CompraMSI` (2 a 60 meses). El gasto se registra completo y sube la deuda por el total. Las mensualidades se **proyectan** (una por corte desde el primer corte posterior a la compra; la última absorbe el redondeo) en `MsiService`, sin crear gastos ficticios.
- **Fecha límite de pago** (`Cuenta.DiasParaPago`, 1 a 60 días después del corte, opcional, solo crédito): `CuentaDetalleService.CalcularPago` calcula el pago para no generar intereses del último corte = deuda al corte − mensualidades MSI aún no facturadas − pagos de tarjeta hechos desde el corte. Se muestra en el detalle de la tarjeta y como aviso en Inicio mientras quede algo pendiente.
- **Recurrentes** (`Recurrente`, solo `Gasto` e `Ingreso`; frecuencia `S` semanal, `Q` 15 y fin de mes, `M` mensual, `A` anual): no mueven saldos por sí solas. Al llegar `ProximaFecha` aparecen como pendientes; confirmar crea una operación normal con `OperacionService.Save` (mismas reglas de saldo, monto ajustable) y avanza `ProximaFecha` en el mismo guardado; omitir solo avanza. `FechaInicio` fija el día del mes (`FrecuenciaRecurrente.Siguiente`); `ProximaFecha` es token de concurrencia para no duplicar al confirmar dos veces.
- **Presupuestos** (`Presupuesto`): monto fijo mensual por categoría (uno por usuario y categoría). Lo gastado son los `Gasto` del mes en esa categoría (un MSI cuenta completo en el mes de la compra). Nivel: menos de 80 % bueno, 80–100 % aviso, 100 % o más alerta; alimenta Inicio y las alertas de Análisis (`PresupuestoService.GetResumen`).
- **Préstamos y deudas**: siempre ligados a una `Persona`, solo con cuentas de débito o efectivo. Por cobrar = prestado − abonos recibidos; por pagar = recibido − pagos hechos. Ninguno puede quedar negativo: no se puede abonar de más ni borrar o editar un préstamo o deuda que ya tiene abonos o pagos.

## Funcionalidad (pantallas)

- **Inicio** (dashboard): ingresos, gastos y diferencia del mes, dinero propio vs. crédito disponible, deuda de tarjetas, gastos por categoría (dona), comparación de 6 meses, indicadores de salud financiera.
- **Movimientos**: alta, edición y baja de operaciones con filtros por fecha, tipo, cuenta y categoría.
- **Cuentas** y detalle de cuenta: saldos, movimientos de la cuenta y, en tarjetas, límite, uso, consumo del periodo y cortes.
- **Meses sin intereses**: total por cobrar, compromisos por mes y avance de cada compra.
- **Análisis**: meses en déficit, variación mensual, gasto esencial vs. no esencial (`Categoria.ClasificacionGasto`: `E`, `N`, `X`), categorías que suben, alertas y simulador de compra grande (contado o MSI).
- **Recurrentes**: plantillas de gastos e ingresos que se repiten, con los pendientes por confirmar (también visibles en Inicio).
- **Presupuestos**: límite mensual por categoría con barras de avance (`Components/BarraPresupuesto`), navegación por mes.
- **Categorías** (`TipoSugerido` `I`/`G`): las generales (`UsuarioId` NULL) son de todos y de solo lectura; cada usuario puede crear, editar y desactivar las suyas (`UsuarioId` = él), que solo él ve. El nombre no puede repetir una general ni otra propia. Todo lugar que valide o liste categorías debe filtrar `UsuarioId == null || UsuarioId == idUsuario` y **Personas** con su detalle e historial de préstamos.

## Endpoints

`api/Usuario`, `api/Cuenta` (incluye `GetDetalle`, `RegistrarCorte`, `EliminarCorte`), `api/Categoria` (`GetList` acepta `incluirInactivas`), `api/Persona`, `api/Operacion`, `api/Dashboard`, `api/Msi`, `api/Analisis`, `api/Recurrente` (`GetPendientes`, `Confirmar`, `Omitir`), `api/Presupuesto` (`GetResumen`). Los métodos de lectura son `Get`/`GetList`/`GetResumen`…; los de escritura, `Save`/`Edit`/`Delete` por POST.

## Al probar con datos

Crear datos de prueba por la API o la interfaz y **borrarlos al terminar** (las operaciones se borran de la más reciente a la más antigua para que los saldos no queden negativos). Las cuentas de prueba (Efectivo, BBVA Debito, Visa) y las personas Ana y Luis pueden seguir en la base local.
