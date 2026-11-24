# Finexa — Fase 1: inventario del diseño actual y plan

Solo lectura del cliente Blazor (`Finanzas/Finanzas`). No se modificó código.

## 1. Estado actual

**Stack visual:** Bootstrap 5.3.3 local (`wwwroot/lib`), Poppins (Google Fonts), iconos Bootstrap Icons en SVG incrustado (sin librería), gráficas SVG/CSS propias. Sin ninguna biblioteca de componentes ni de gráficas. No existe tema oscuro, selector de tema ni pantalla de Configuración.

**Hojas de estilo**
| Archivo | Líneas | Rol |
|---|---|---|
| `wwwroot/css/app.css` | 541 | Variables `--fz-*`, sobrescrituras de Bootstrap, tablas-tarjeta móvil, modales pantalla completa, formulario de movimiento por pasos |
| `Layout/MainLayout.razor.css` | 94 | Shell: sidebar 250px (≥641px), top-row, avatar |
| `Layout/NavMenu.razor.css` | 123 | Menú: iconos como `background-image` data-URI blancos, breakpoint 641px |
| `Layout/LoginLayout.razor.css` | 8 | Fondo con `imagenFondo.png` (1.5 MB) |
| `Pages/Home.razor.css` (171), `Analisis.razor.css` (121), `Msi.razor.css` (39) | | Gráficas y tarjetas de esas pantallas |

**Layouts:** `MainLayout` (sidebar + top-row con usuario y "Salir" + contenido), `LoginLayout` (tarjeta de 400px centrada; lo usan Login y Registro).

**Pantallas** (`Pages/`, líneas razor+cs): Home 324, Movimientos 352, Cuentas 328, CuentaDetalle 369, Recurrentes 427, Presupuestos 303, Msi 170, Analisis 348, Categorias 277, Personas 209, PersonaDetalle 268, Login 102, Registro 145.
**Componentes:** `MovimientoForm` (489, modal de alta/edición, por pasos en móvil), `PendientesRecurrentes`, `BarraPresupuesto`, `IconoAccion` (detalle/editar/copiar/eliminar).
**Modales hechos a mano** en 8 archivos (Categorias, Cuentas, Movimientos, PersonaDetalle, Personas, Presupuestos, Recurrentes, MovimientoForm).

## 2. Hallazgos que condicionan el rediseño

1. **Base reutilizable:** ya existe `--fz-*` y los breakpoints móviles. Se puede migrar por renombrado/ampliación de tokens en vez de reescribir.
2. **Colores fijos que romperían el modo oscuro** (≈70 hex/rgba en CSS, 6 en razor/cs):
   - `Home.razor.cs:13` paleta de la dona en hex; `Home.razor:141` pista de la dona `#e9f1ee`; `Home.razor:187` y `Analisis.razor:64` puntos de leyenda en hex.
   - `Login.razor:6` y `Registro.razor:6` fondo `rgba(255,255,255,.92)` inline.
   - Muchos `background:#fff` en CSS de página y `app.css` (tarjetas, `.tipo-boton`, `.alerta-fila`).
3. **Bootstrap pinta con sus propios colores:** utilidades `bg-white` (9), `bg-success/secondary/warning/info/danger`, `text-dark`, `alert-*` (≈34) y `text-success/danger` (34). Bootstrap 5.3 soporta `data-bs-theme="dark"`, pero estas clases hay que revisarlas una por una (p. ej. `text-dark` sobre badges amarillos, `bg-white`).
4. **Menú:** los iconos son data-URI con `fill='white'` incrustado; no se pueden recolorear con tokens. Conviene pasarlos a SVG con `currentColor` (como ya hace `IconoAccion`).
5. **Navegación móvil:** hoy es el sidebar colapsable con hamburguesa (<641px). La barra inferior es nueva; el menú "Más" también.
6. **Breakpoints inconsistentes:** 575.98 / 640.98 / 991.98 en distintos archivos. Definir uno estándar (móvil < 576, tablet 576–991, escritorio ≥ 992) y aplicarlo al layout (hoy el sidebar aparece desde 641px, sin modo tablet reducido).
7. **Marca:** "Finanzas" aparece en `index.html` (`<title>`), `NavMenu.razor`, `PageTitle` de Login/Registro y alt de logos. Assets actuales: `Logo.png` (752 KB), `favicon.png`, `icon-192.png`. `theme-color` es `#0f4c45`.
8. **Gráficas:** barras de Home/Analisis/Msi son `div` con `height:%`; dona es SVG. Colores de la dona vienen de C#. `BarraPresupuesto` usa clases Bootstrap `progress-bar bg-*`.
9. **Tablas:** `table-cards` ya convierte tablas en tarjetas bajo 992px con `data-label`: base sólida para móvil y tablet.
10. **Referencias:** las imágenes muestran "Hola, Prueba", buscador, campana de notificaciones, selector de mes, tarjeta "Consejo financiero" y "Movimientos recientes / Tus cuentas" en Inicio. Nada de eso existe hoy (buscador global, notificaciones, consejo). El spec dice que no hay módulos funcionales nuevos, así que propongo tratarlos como **opcionales/visuales** (ver preguntas).

## 3. Comportamientos que NO deben cambiar
- Atributos `data-label`, clase `acciones` y clases `.table-cards` (los usa el CSS móvil).
- Flujo por pasos de `MovimientoForm` y sus clases `mov-*`, `tipo-boton`, `cuenta-botones`; `EditForm` dentro de `.modal-content` (el CSS móvil depende de `> form`).
- `FocusOnNavigate` sobre `h1` en `App.razor`.
- `AuthorizeView`, `RedirectToLogin`, `CustomAuthProvider`, `AuthHttpHandler`: solo se toca marcado/estilo, no lógica.
- Formato de importes `Formato.Moneda` y `CultureInfo.InvariantCulture` en estilos inline.
- Colores semánticos: ingreso verde, gasto rojo, y `BarraPresupuesto` nivel bueno/aviso/alerta.

## 4. Plan propuesto (Fases 2–3)

**Fase 2 — Identidad y tokens**
1. Preparar assets desde tu logo (vectorizar o recortar el PNG): isotipo, completo claro, completo oscuro, monocromático, favicon e icon-192. *El PNG de referencia tiene fondo blanco y degradados; para variantes limpias lo ideal es un SVG.*
2. Nuevo `wwwroot/css/tokens.css` con `:root` (claro) y `[data-bs-theme="dark"]` (oscuro): marca `#104E46/#168875/#36B985/#EAF5F1/#202A32`, superficies, bordes, texto, semánticos, radios, sombras, espaciados. Mantener alias `--fz-*` temporalmente para no romper páginas.
3. Reescribir `app.css` para que Bootstrap use esos tokens (`--bs-body-bg`, `--bs-body-color`, `--bs-border-color`, `--bs-primary`…) y clases base (botón, tarjeta, tabla, badge, modal, formulario).

**Fase 3 — Estructura global**
1. Servicio de tema (`ThemeService` + JS mínimo o `IJSRuntime` con `Blazored.LocalStorage`, ya instalado) que pone `data-bs-theme` en `<html>`; script en `index.html` que lo aplica antes de pintar para evitar parpadeo.
2. Pantalla **Configuración** nueva con selector Claro / Oscuro / Sistema.
3. `MainLayout`: sidebar con marca Finexa e iconos `currentColor`, encabezado con usuario; modo tablet con sidebar compacto; móvil con barra inferior (Inicio, Movimientos, Agregar, Cuentas, Más) y hoja "Más".
4. Botón **Agregar** que abra `MovimientoForm` desde cualquier pantalla (hoy se abre desde Movimientos; hay que decidir cómo exponerlo globalmente sin cambiar su lógica).

Luego Fase 4 (pantallas, en el orden del documento) y Fases 5–6.

## 5. Decisiones pendientes
1. **Logo:** hay dos versiones en las imágenes (la hoja con la "F" y la hoja con flecha y barras). ¿Cuál es la definitiva? ¿Existe archivo vectorial/PNG transparente?
2. **Extras de las referencias** (buscador, campana, "Consejo financiero", "Movimientos recientes" y "Tus cuentas" en Inicio): ¿se incluyen como elementos visuales sin lógica nueva, o se omiten por el alcance?
3. **Tipografía:** las imágenes parecen Poppins/Inter; ¿mantenemos Poppins?
4. **Nombre en base de datos/API/proyectos** (`Finanzas.sln`, namespaces): solo cambiar el nombre visible, ¿correcto?
