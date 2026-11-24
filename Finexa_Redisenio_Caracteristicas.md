# Finexa --- Especificación del rediseño visual y de identidad

**Nombre del producto:** Finexa\
**Eslogan:** Tu dinero, en control\
**Tipo de documento:** Alcance y características del cambio visual\
**Estado:** Propuesta aprobada como dirección de diseño\
**Fecha:** 24 de noviembre de 2026

------------------------------------------------------------------------

## 1. Objetivo del cambio

Renovar la identidad visual y la experiencia de uso de la aplicación
personal de finanzas, que actualmente se presenta como **Finanzas**,
para convertirla en **Finexa**.

El rediseño busca una apariencia elegante, moderna y premium, inspirada
en productos fintech, sin reconstruir la aplicación desde cero ni
alterar la lógica de negocio que ya funciona.

El cambio contempla:

-   Nueva identidad de marca: nombre, logotipo, eslogan y paleta de
    colores.
-   Rediseño visual de todos los módulos existentes.
-   Experiencia adaptable a escritorio, tablet y móvil.
-   Modo claro y modo oscuro.
-   Componentes visuales consistentes.
-   Conservación de las funcionalidades, datos y reglas actuales.

## 2. Identidad de marca

### 2.1 Nombre

**Finexa**

El nombre sustituye a "Finanzas" en la interfaz y en los elementos
visibles de la aplicación.

### 2.2 Eslogan

**Tu dinero, en control.**

Puede utilizarse en la pantalla de inicio de sesión, materiales de
identidad y espacios donde aporte valor sin recargar la interfaz.

### 2.3 Concepto del logotipo

La dirección visual aprobada es un símbolo geométrico que combina:

-   Una forma orgánica inspirada en una hoja o trayectoria de
    crecimiento.
-   Barras ascendentes que representan progreso financiero.
-   Una composición sencilla que pueda reconocerse en tamaños pequeños.

El logotipo deberá contar con variantes:

1.  **Completo:** símbolo y palabra Finexa.
2.  **Isotipo:** únicamente el símbolo, para favicon, menú y accesos
    directos.
3.  **Para fondo claro:** versión con contraste adecuado sobre
    superficies claras.
4.  **Para fondo oscuro:** versión con contraste adecuado sobre
    superficies oscuras.
5.  **Monocromático:** para usos donde no sea posible utilizar la paleta
    completa.

Los mockups generados son referencias visuales; los archivos finales del
logotipo deberán prepararse en formatos apropiados para la aplicación.

## 3. Sistema visual

### 3.1 Paleta de marca propuesta

  Uso                 Color                     Código
  ------------------- ------------------------- -----------
  Verde profundo      Principal oscuro          `#104E46`
  Verde esmeralda     Principal                 `#168875`
  Verde crecimiento   Acento                    `#36B985`
  Fondo claro         Superficie general        `#EAF5F1`
  Grafito             Texto/superficie oscura   `#202A32`

Los colores deberán centralizarse como variables de diseño. No se
recomienda repetir valores hexadecimales directamente en cada
componente.

### 3.2 Colores semánticos

Se conservará una codificación fácil de reconocer:

-   Ingresos y valores positivos: verde.
-   Gastos y valores negativos: rojo.
-   Información neutral: gris o grafito.
-   Acciones principales: verde esmeralda.
-   Advertencias: tonos ámbar, utilizados con moderación.
-   Elementos informativos: tonos azules o turquesa cuando sea
    necesario.

El color nunca deberá ser el único medio para comunicar el significado;
se acompañará de texto, iconos o etiquetas.

### 3.3 Tipografía

Utilizar una tipografía moderna, legible y consistente en toda la
aplicación. Mantener jerarquías claras para:

-   Títulos de página.
-   Encabezados de tarjetas y secciones.
-   Valores monetarios.
-   Etiquetas y textos secundarios.
-   Campos y acciones de formularios.

Los importes financieros deberán tener especial legibilidad y alineación
consistente.

### 3.4 Componentes y superficies

-   Tarjetas con bordes suaves y sombras discretas.
-   Radios de borde consistentes.
-   Espaciado uniforme.
-   Botones con estados normal, hover, foco, presionado y deshabilitado.
-   Campos de formulario con etiquetas visibles y estados de validación.
-   Tablas limpias, con encabezados diferenciados.
-   Etiquetas de estado y tipo de movimiento fáciles de identificar.
-   Iconografía de una misma familia visual.
-   Animaciones breves y sutiles, sin afectar el rendimiento.

## 4. Modo claro y modo oscuro

Ambos temas forman parte del alcance inicial del rediseño. No serán
diseños independientes: utilizarán los mismos componentes y estructura,
cambiando sus tokens visuales.

### 4.1 Modo claro

-   Fondos generales claros, con matiz verde muy suave.
-   Tarjetas blancas.
-   Texto principal en grafito o verde profundo.
-   Acciones y estados positivos en verde esmeralda.
-   Bordes discretos para separar superficies.

### 4.2 Modo oscuro

-   Fondo general grafito o verde muy oscuro.
-   Tarjetas y paneles en tonos oscuros diferenciados del fondo.
-   Texto principal claro y texto secundario con contraste suficiente.
-   Verde esmeralda como color de acción y acento.
-   Gráficas, tablas, formularios, modales y estados adaptados al tema.

### 4.3 Selector y persistencia

-   Incluir un selector de tema en Configuración.
-   Permitir cambiar entre claro y oscuro sin recargar innecesariamente
    la experiencia.
-   Aplicar el tema a todas las pantallas y componentes.
-   Conservar la preferencia en el dispositivo del usuario.
-   Si posteriormente se desea sincronizar la preferencia entre
    dispositivos, podrá almacenarse también en el perfil del usuario.

La persistencia local y la sincronización entre dispositivos son
comportamientos distintos; la sincronización no se considera
implementada por el solo hecho de guardar la preferencia en el
navegador.

## 5. Diseño adaptable: escritorio, tablet y móvil

El diseño deberá responder al ancho disponible y no limitarse a reducir
proporcionalmente la versión de escritorio.

### 5.1 Escritorio

-   Mantener una navegación lateral.
-   Encabezado superior con información de usuario y acciones generales.
-   Área principal con anchura y espaciado equilibrados.
-   Tarjetas e indicadores distribuidos en cuadrículas adaptables.
-   Tablas completas cuando exista espacio suficiente.

### 5.2 Tablet

-   Reducir el ancho de la navegación lateral o permitir contraerla.
-   Reorganizar tarjetas en menos columnas.
-   Evitar tablas cortadas; permitir desplazamiento horizontal
    controlado o una presentación alternativa.
-   Mantener controles táctiles cómodos.

### 5.3 Móvil

-   Utilizar navegación inferior para las secciones principales.
-   Incorporar un acceso destacado para agregar un movimiento.
-   Presentar tarjetas en una sola columna o cuadrículas compactas.
-   Convertir tablas extensas en listas o tarjetas cuando mejore la
    lectura.
-   Mostrar filtros mediante paneles o controles desplegables.
-   Mantener botones y campos adecuados para interacción táctil.
-   Evitar desplazamiento horizontal de toda la pantalla.

La navegación móvil propuesta contempla accesos directos a **Inicio**,
**Movimientos**, **Agregar**, **Cuentas** y **Más**. Los módulos
restantes estarán disponibles dentro de "Más" o de una navegación
secundaria.

## 6. Estructura y módulos incluidos

El rediseño abarca toda la aplicación y contempla los módulos que ya
aparecen en la interfaz actual.

### 6.1 Inicio / Dashboard

Conservar y mejorar la presentación de:

-   Ingresos del mes.
-   Gastos del mes.
-   Diferencia entre ingresos y gastos.
-   Dinero propio (efectivo y débito).
-   Crédito disponible.
-   Deuda en tarjetas.
-   Gastos por categoría.
-   Ingresos y gastos por mes.

Mejoras visuales:

-   Jerarquía más clara entre indicadores.
-   Importes destacados y fáciles de comparar.
-   Gráficas con leyendas y etiquetas legibles.
-   Estados vacíos bien diseñados cuando no existan movimientos.
-   Selector de mes integrado y fácil de utilizar.
-   Acceso visible a la acción de agregar movimiento.

Se debe conservar la distinción conceptual entre dinero propio y crédito
disponible. El crédito disponible no debe presentarse como dinero
propio.

### 6.2 Movimientos

Conservar la consulta y administración de movimientos.

Mejoras visuales:

-   Filtros de mes, tipo, cuenta y categoría.
-   Tabla más clara en escritorio.
-   Presentación en tarjetas o lista en móvil.
-   Identificación visual de ingresos y gastos.
-   Importe y signo fáciles de reconocer.
-   Acciones de consultar, duplicar, editar y eliminar con iconos
    consistentes y ayudas accesibles.
-   Formularios de alta y edición con distribución uniforme.

### 6.3 Cuentas

Conservar la administración de cuentas y sus tipos actuales:

-   Efectivo.
-   Débito.
-   Crédito.

Mejoras visuales:

-   Resumen del dinero disponible.
-   Resumen de deuda en tarjetas de crédito.
-   Listado de cuentas con nombre, tipo, saldo o deuda, disponible, día
    de corte y estado, según corresponda.
-   Estados activa e inactiva claramente diferenciados.
-   Acciones de consultar, editar y activar/desactivar.
-   Diseño específico para móvil.

### 6.4 Recurrentes

Aplicar el mismo sistema visual a la administración de movimientos
recurrentes:

-   Listados y estados claros.
-   Formularios consistentes con el resto de la aplicación.
-   Fechas, periodicidad e importes fáciles de identificar.
-   Adaptación a móvil y a ambos temas.

No se modifican las reglas funcionales existentes de recurrencia dentro
de este cambio visual.

### 6.5 Presupuestos

Rediseñar la presentación de presupuestos:

-   Resumen del presupuesto.
-   Importe asignado y gasto acumulado, según los datos disponibles.
-   Indicadores visuales de avance.
-   Estados de presupuesto comprensibles.
-   Formularios y listados consistentes.
-   Adaptación a modo oscuro y móvil.

No se agregan reglas de cálculo nuevas como parte del rediseño.

### 6.6 Meses sin intereses

Mejorar la presentación de compras y compromisos a meses sin intereses:

-   Listados claros.
-   Importes y mensualidades fáciles de consultar.
-   Estados y fechas visualmente diferenciados.
-   Resumen de compromisos cuando la información existente lo permita.
-   Diseño adaptable a pantallas pequeñas.

Se conserva la regla financiera existente del proyecto: las compras a
meses sin intereses reducen la disponibilidad de crédito considerando el
importe completo de la compra desde el inicio.

### 6.7 Análisis

Mejorar la lectura de la información financiera:

-   Gráficas consistentes.
-   Comparaciones por periodo.
-   Distribución de gastos por categoría.
-   Ingresos frente a gastos.
-   Leyendas y etiquetas legibles.
-   Estados vacíos cuando no exista información suficiente.

Los gráficos deberán funcionar correctamente en los dos temas y
adaptarse al ancho de pantalla.

### 6.8 Categorías

Conservar la administración de categorías y su clasificación.

Mejoras visuales:

-   Identificación mediante nombre, emoji o icono.
-   Separación visual entre ingresos y gastos.
-   Filtros y búsqueda.
-   Etiquetas de tipo y clasificación.
-   Formularios de alta y edición consistentes.
-   Listados adaptados a móvil.

### 6.9 Personas

Conservar el módulo de personas relacionado con los registros de
préstamos y deudas.

Mejoras visuales:

-   Listados más claros.
-   Importes pendientes fáciles de identificar.
-   Separación visual de préstamos y deudas cuando corresponda.
-   Formularios consistentes.
-   Adaptación a móvil y modo oscuro.

## 7. Formularios, ventanas y elementos compartidos

Todos los módulos deberán utilizar patrones visuales comunes para:

-   Crear y editar registros.
-   Confirmar eliminaciones.
-   Mostrar detalles.
-   Presentar errores y validaciones.
-   Mostrar mensajes de éxito.
-   Estados de carga.
-   Estados sin información.
-   Filtros, buscadores y selectores.
-   Botones principales y secundarios.

Las ventanas modales deberán ser legibles, tener acciones claras y
adaptarse a pantallas pequeñas. En móvil podrá utilizarse una
presentación de tipo panel inferior cuando sea conveniente.

## 8. Navegación y encabezado

### 8.1 Escritorio

-   Marca Finexa en la parte superior del menú lateral.
-   Navegación con iconos uniformes.
-   Elemento activo claramente identificado.
-   Encabezado con usuario y acciones generales.
-   Acceso a Configuración y selector de tema, según la estructura final
    del proyecto.

### 8.2 Móvil

-   Navegación inferior para las secciones prioritarias.
-   Botón central o destacado para agregar movimiento.
-   Acceso a los demás módulos mediante "Más".
-   Menor cantidad de elementos simultáneos para evitar saturación.

## 9. Pantalla de inicio de sesión

Actualizar la pantalla actual para reflejar la marca Finexa:

-   Logotipo y nombre Finexa.
-   Eslogan "Tu dinero, en control".
-   Fondo e ilustración coherentes con la identidad.
-   Formulario limpio y centrado.
-   Campos de correo y contraseña.
-   Botón principal en verde esmeralda.
-   Enlace para crear cuenta, si la funcionalidad está habilitada.
-   Estados de validación y error.
-   Adaptación a escritorio y móvil.
-   Compatibilidad con modo claro y oscuro si el flujo de autenticación
    permite aplicar la preferencia.

El rediseño no cambia el mecanismo de autenticación.

## 10. Reglas para conservar el funcionamiento actual

Este proyecto es un rediseño de identidad y experiencia, no una
reconstrucción funcional.

Por lo tanto:

-   No eliminar módulos existentes.
-   No eliminar ni cambiar datos almacenados.
-   No modificar contratos de API por motivos exclusivamente visuales.
-   No cambiar cálculos de saldos, ingresos, gastos, crédito o deuda.
-   No cambiar reglas de movimientos, cuentas, categorías, recurrencias,
    presupuestos, MSI o personas.
-   No modificar permisos o comportamiento de autenticación.
-   No reemplazar componentes funcionales sin verificar que se conserven
    sus eventos y validaciones.
-   Mantener compatibilidad con la estructura actual de Blazor y la API.
-   Separar los cambios visuales de los cambios de lógica.

Cualquier mejora funcional detectada durante el trabajo deberá
documentarse y aprobarse por separado.

## 11. Lineamientos técnicos recomendados para Blazor

La implementación deberá adaptarse a la arquitectura real del proyecto.
Como guía:

1.  Centralizar colores, tipografía, bordes, sombras y espaciados en
    variables CSS.
2.  Definir tokens para tema claro y oscuro.
3.  Aplicar el tema desde un atributo o clase en el elemento raíz de la
    aplicación.
4.  Crear estilos reutilizables para botones, tarjetas, tablas,
    formularios, etiquetas y modales.
5.  Evitar estilos duplicados por página.
6.  Utilizar componentes compartidos cuando exista un patrón repetido.
7.  Implementar breakpoints consistentes para escritorio, tablet y
    móvil.
8.  Revisar las bibliotecas de componentes y gráficas ya utilizadas
    antes de sustituirlas.
9.  Mantener los estilos aislados o globales de acuerdo con la
    organización actual del proyecto.
10. Verificar contraste, foco de teclado, etiquetas y estados
    accesibles.

No se presupone una biblioteca CSS o de componentes específica. La
elección deberá hacerse después de revisar el proyecto existente.

## 12. Fases propuestas de implementación

### Fase 1 --- Revisión y preparación

-   Revisar estructura actual de Blazor.
-   Identificar layouts, páginas, componentes y hojas CSS.
-   Identificar librerías de iconos y gráficas.
-   Registrar comportamientos que no deben cambiar.
-   Preparar inventario de pantallas.

**Resultado:** mapa técnico del diseño actual y plan de modificación.

### Fase 2 --- Identidad y sistema visual

-   Incorporar marca Finexa.
-   Preparar variantes del logotipo.
-   Definir variables de color y tipografía.
-   Crear tokens de modo claro y oscuro.
-   Definir componentes base.

**Resultado:** identidad y sistema visual reutilizable.

### Fase 3 --- Estructura global

-   Rediseñar layout de escritorio.
-   Rediseñar navegación móvil.
-   Actualizar encabezado.
-   Implementar selector y persistencia del tema.

**Resultado:** estructura común aplicada a toda la aplicación.

### Fase 4 --- Pantallas y módulos

Aplicar el sistema visual a:

1.  Inicio.
2.  Movimientos.
3.  Cuentas.
4.  Recurrentes.
5.  Presupuestos.
6.  Meses sin intereses.
7.  Análisis.
8.  Categorías.
9.  Personas.
10. Inicio de sesión y pantallas relacionadas.

**Resultado:** módulos con una apariencia uniforme.

### Fase 5 --- Adaptación móvil y accesibilidad

-   Revisar cada pantalla en móvil y tablet.
-   Adaptar tablas y formularios.
-   Ajustar áreas táctiles.
-   Revisar contraste y navegación por teclado.
-   Corregir desbordamientos y problemas de lectura.

**Resultado:** experiencia responsive y accesible.

### Fase 6 --- Pruebas y validación

-   Comparar resultados y saldos antes y después.
-   Probar altas, consultas, ediciones y eliminaciones.
-   Probar filtros y navegación.
-   Probar modo claro y oscuro en cada módulo.
-   Probar sesiones y autenticación.
-   Revisar escritorio, tablet y móvil.
-   Corregir regresiones visuales o funcionales.

**Resultado:** rediseño validado sin pérdida de funcionalidades.

## 13. Criterios de aceptación

El rediseño podrá considerarse terminado cuando:

-   La interfaz muestre la marca Finexa de manera consistente.
-   El logotipo tenga variantes adecuadas para fondos claros y oscuros.
-   Todos los módulos existentes tengan el nuevo diseño.
-   El modo claro y oscuro funcionen en todas las pantallas.
-   La preferencia de tema se conserve en el dispositivo.
-   La aplicación sea utilizable en escritorio, tablet y móvil.
-   Las tablas, gráficas, formularios y modales sean legibles en ambos
    temas.
-   No se pierdan funcionalidades existentes.
-   Los cálculos y datos financieros se mantengan sin cambios.
-   No existan errores importantes de navegación, visualización o
    interacción.

## 14. Elementos fuera del alcance de este cambio

Salvo aprobación posterior, no forman parte de este rediseño:

-   Reconstruir la API.
-   Cambiar la base de datos.
-   Cambiar el sistema de autenticación.
-   Integrar bancos o sincronización bancaria automática.
-   Incorporar nuevas reglas financieras.
-   Cambiar el modelo de cuentas o movimientos.
-   Añadir módulos funcionales nuevos.
-   Sincronizar automáticamente el tema entre dispositivos.

Estos elementos pueden evaluarse como proyectos independientes.

## 15. Resumen ejecutivo

**Finexa** será la nueva identidad de la aplicación personal de
finanzas, con el eslogan **"Tu dinero, en control"**.

El rediseño será integral y tendrá una estética elegante y premium,
basada en verdes esmeralda, turquesa, superficies limpias y una
identidad gráfica asociada al crecimiento financiero.

La aplicación contará con:

-   Marca y logotipo Finexa.
-   Diseño premium para todos los módulos.
-   Modo claro y modo oscuro.
-   Experiencia responsive para escritorio, tablet y móvil.
-   Navegación móvil optimizada.
-   Componentes visuales reutilizables.
-   Conservación de las funcionalidades, datos y reglas de negocio
    actuales.

**Principio rector:** modernizar la experiencia de uso sin poner en
riesgo lo que ya funciona.
