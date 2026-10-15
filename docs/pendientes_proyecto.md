# Pendientes del proyecto — QA exhaustivo de módulos

Registro de hallazgos del QA exhaustivo realizado sobre el código real, módulo por módulo,
contra `.claude/hikari-references/modulos/*.md`, el enunciado del TFG y la documentación de BD.

No se repiten aquí los hallazgos ya registrados en `.claude/hikari-references/incoherencias_a_revisar.md`
ni los "Pendientes y bugs conocidos" que ya trae cada doc de módulo — solo se anota si cambia su estado
(resuelto/reabierto) o si aparece algo nuevo. Cada entrada indica módulo, sección exacta (archivo:línea)
y contra qué fuente contradice.

Formato por hallazgo:
- **Qué se esperaba** (fuente + cita)
- **Qué hace el código hoy** (archivo:línea)
- **Por qué importa**
- **Acción sugerida / pendiente**

**Estado de cada hallazgo:** ⏳ Pendiente (por defecto, sin marca) · ✅ **RESUELTO** (con la fecha y
dónde se corrigió) · 📝 Solo corrección de documentación (no afecta al sistema en ejecución).

---

## Hallazgos transversales (afectan a más de un módulo / a la planificación del QA)

### T.1 — Reportes (RF-012) y Evaluación de Calidad (RF-013) ya tienen código real; el README de `modulos/` dice lo contrario

- **Qué dice la documentación** — `.claude/hikari-references/modulos/README.md`, sección "Módulos NO
  documentados aquí (sin código todavía)": lista "Reportes y analítica (RF-012)" y "Evaluación de
  Calidad (RF-013)", con la nota "Se documentarán aquí igual que los demás cuando tengan
  controlador/service real que revisar."
- **Qué hay en el código hoy** — Ya existen, completos: `Controllers/Reportes/ReportesController.cs`,
  `Controllers/Calidad/CalidadController.cs`, `Services/Implementations/ReporteService.cs` +
  `IReporteService.cs`, `Services/Implementations/EvaluacionCalidadService.cs` +
  `IEvaluacionCalidadService.cs`, `Views/Reportes/`, `Views/Calidad/`. Confirmado también por
  `Constants/Permisos.cs` (clases `Reportes` con 7 códigos y `Calidad` con 2 códigos) y por los commits
  recientes en el historial: `728bcb0` (HU-030, carga por colaborador), `c484676` (HU-031, comparativo
  de servicios), `bbe021c` (HU-032, evaluación de calidad post-servicio), `00134e7` (HU-033,
  rentabilidad por expediente).
- **Por qué importa** — El README de `modulos/` es el punto de entrada que este mismo QA (y cualquier
  testing manual futuro) usa para saber qué revisar; con esta desactualización, alguien podría saltarse
  por completo el QA de dos módulos que sí están en producción-de-pruebas. Detectado el 2026-09-24 al
  cruzar el catálogo de permisos contra la lista de módulos documentados durante el QA del módulo 01.
- **Acción sugerida** — (a) Sacar ambas líneas de la sección "NO documentados" del README; (b) crear
  `modulos/13-reportes.md` y `modulos/14-calidad.md` siguiendo el mismo formato que los demás (RF,
  modelo real, reglas de negocio, diagrama Mermaid, checklist); (c) hacerles su propio QA — ya están
  en la cola de este documento (ver índice de revisión abajo, módulos 13 y 14). Nota: `Reportes.Conversion`,
  `Reportes.Ingresos` y `Reportes.Geo` son 3 de los 7 códigos de permiso del módulo Reportes que **no**
  corresponden a ninguno de los 4 HU ya confirmados (030/031/032/033) — hay que verificar en el QA del
  módulo 13 si esos 3 reportes también tienen controlador/vista real o si son permisos "adelantados"
  sin funcionalidad todavía (posible permiso huérfano).
  **Actualización 2026-09-24, ya verificado en el QA del módulo 13 (abajo): no son huérfanos.** Los 7
  permisos de `Reportes` (incluidos `Conversion`, `Ingresos` y `Geo`) están cableados a 7 acciones
  reales en `ReportesController.cs` con reportes completos en `ReporteService.cs` (532 líneas) — el
  módulo RF-012 está más completo de lo que sugerían los commits HU-030/031/033 por sí solos.

### T.2 — Patrón repetido: permisos declarados y sembrados por defecto que ningún `[Permiso(...)]` usa

Confirmados hasta ahora, cada uno ya con su propia nota en el módulo que le corresponde — se listan
juntos aquí solo porque el patrón se repite igual en tres módulos distintos, no porque sea un hallazgo
nuevo en sí:

| Permiso huérfano | Módulo | Rol(es) que lo tienen por defecto |
|---|---|---|
| `Prospectos.Calificar` | 03 Prospectos | Abogado/Asesor, Asistente |
| `Clientes.Crear` | 04 Clientes | Abogado/Asesor (+ Administrador vía sincronización completa) |
| `Expedientes.Asignar` | 07 Expedientes | Abogado/Asesor (+ Administrador) |

- **Por qué importa** — Ninguno de los tres rompe nada (el rol simplemente tiene un permiso que nunca
  se evalúa), pero infla la pantalla de "Gestionar permisos" de Roles con casillas que no hacen nada
  si se marcan/desmarcan, lo que puede confundir al Administrador al configurar un rol nuevo.
- **Acción sugerida** — Decidir con el usuario, para los tres casos: (a) implementar el chequeo que
  falta (p. ej. `Prospectos.Calificar` como permiso real y separado de `Editar`), o (b) eliminarlos
  del catálogo si se acepta que la calificación/creación/asignación quedan cubiertas por permisos más
  generales. No se propone una acción única porque cada caso puede tener una respuesta distinta.

### T.3 — Rediseño completo del mecanismo Pro Bono (2026-09-18): ya no es "Cliente.ModalidadPago pegajoso", es una reserva por Propuesta — no documentado en ningún doc de módulo (afecta 06, 08 y 12)

- **Qué documentan hoy los módulos afectados:**
  - `modulos/12-probono.md` §4/§5: "al aprobar una solicitud cuyo beneficiario es un Cliente, el
    sistema actualiza automáticamente `Cliente.ModalidadPago = ProBono`... Esto es lo que hace que...
    `ExpedienteService.CerrarExpediente` genere la factura... con `MontoTotal = 0`".
  - `modulos/08-facturacion.md` §4 (ver hallazgo 8.1 arriba): sigue describiendo `Cliente.ModalidadPago`
    como fuente de la modalidad de la factura.
  - `modulos/06-propuestas.md`: no menciona ninguna restricción para marcar una propuesta como
    "Pro Bono" — según ese doc, cualquiera con `Propuestas.Crear`/`Editar` podría elegir esa modalidad
    libremente.
- **Qué hace el código hoy (los tres módulos ya fueron rediseñados juntos, mismo día, 2026-09-18):**
  - `SolicitudProBono` ganó un campo nuevo, `PropuestaConsumidaId` (`Models/SolicitudProBono.cs:31`,
    nullable, FK a `Propuesta`) — no existe en el modelo documentado en `modulos/12-probono.md` §2.
  - `ProBonoService.ResolverInterno` (línea 195-245) **ya no toca `Cliente.ModalidadPago`** al aprobar
    — el propio código lo dice explícitamente en un comentario (línea 219-227): la versión anterior
    "marcaba al cliente entero como pro bono para siempre, así que cualquier expediente futuro de ese
    cliente, sin relación con esta solicitud, también facturaba en cero" (el mismo problema, con las
    mismas palabras, que motivó el cambio de fuente en `ExpedienteService` — ver hallazgo 8.1).
  - `PropuestaService.ValidarYReservarSolicitudProBono` (línea 639-649) es la nueva puerta: una
    Propuesta solo puede marcarse "Pro Bono" si existe una `SolicitudProBono` **Aprobada y sin usar**
    (`PropuestaConsumidaId == null`) para ese mismo Cliente/Prospecto — si no existe, lanza
    `ReglaNegocioException` ("No hay una solicitud pro bono aprobada y disponible para este
    beneficiario..."). Al crear/editar la propuesta con esa modalidad, la solicitud queda "consumida"
    (`solicitud.PropuestaConsumidaId = propuesta.PropuestaId`, línea 239/450); al editar una propuesta
    para quitarle la modalidad Pro Bono o cambiar de destinatario, la reserva se libera (línea 430-436)
    para poder usarse en otra propuesta.
  - Encadenado con el hallazgo 8.1: `ExpedienteService.CerrarExpedienteInterno` decide el monto cero
    según `Propuesta.ModalidadPago` (ya reservada/validada en este mecanismo), no `Cliente.ModalidadPago`.
- **Por qué importa** — Es un rediseño real y bien pensado (corrige exactamente el defecto que el
  propio equipo detectó: un cliente quedaba "pro bono para siempre"), pero **ningún doc de módulo lo
  describe** — quien pruebe el flujo Pro Bono guiándose por `modulos/12-probono.md` esperaría ver el
  badge "Pro Bono" en la ficha del Cliente inmediatamente después de aprobar, cosa que ya no ocurre; y
  quien lea `modulos/06-propuestas.md` no sabría que existe una validación adicional al marcar una
  propuesta como Pro Bono (mensaje de error nuevo, no listado en ese doc).
- **Acción sugerida** — Reescribir `modulos/12-probono.md` §2/§4/§5 (quitar la actualización automática
  de `Cliente.ModalidadPago`, documentar `PropuestaConsumidaId` y el ciclo reservar/consumir/liberar) y
  agregar a `modulos/06-propuestas.md` una nota sobre la validación de solicitud pro bono aprobada y
  disponible al marcar/editar una propuesta como "Pro Bono". Ya cubierto en 8.1 para el lado de
  Facturación.

---

## Índice de revisión

| Módulo | Estado QA | Fecha | Hallazgos nuevos |
|---|---|---|---|
| 01 Roles, Permisos, Usuarios | Revisado | 2026-09-24 | 3 (2 correcciones al doc, 1 gap de documentación de proyecto) |
| 02 Direcciones | Revisado | 2026-09-24 | 0 (todo coincide con el doc) |
| 03 Prospectos | Revisado | 2026-09-24 | 1 (corrección menor al doc, mismo texto que incoherencias #12/#13) |
| 04 Clientes | Revisado | 2026-09-24 | 0 (todo coincide con el doc y con incoherencias #7/#8 ya registradas) |
| 05 Servicios | Revisado | 2026-09-24 | 0 (todo coincide con el doc) |
| 06 Propuestas | Revisado | 2026-09-24 | 0 (todo coincide con el doc, incluido el bug de zona horaria) |
| 07 Expedientes/Kanban | Revisado | 2026-09-24 | 0 (todo coincide; módulo más grande, verificación por muestreo dirigido) |
| 08 Facturación | Revisado | 2026-09-24 | 2 (1 importante: doc contradice al código en la fuente de `ModalidadPago`; 1 doc totalmente obsoleto, confirma advertencia del README) |
| 09 Notificaciones | Revisado | 2026-09-24 | 0 (todo coincide con el doc) |
| 10 Dashboard | Revisado | 2026-09-24 | 0 (todo coincide con el doc) |
| 11 Auditoría | Revisado | 2026-09-24 | 2 (✅ incoherencias #9 y #10 resueltas y ya commiteadas — no nuevas) |
| 12 Pro Bono | Revisado | 2026-09-24 | 1 importante (ver T.3, rediseño no documentado, cruza 06/08/12) |
| 13 Reportes (RF-012) | Revisado (sin doc previo — ver T.1) | 2026-09-24 | 0 (implementación completa y consistente) |
| 14 Evaluación de Calidad (RF-013) | Revisado (sin doc previo — ver T.1) | 2026-09-24 | 1 (falta trigger de inmutabilidad ya previsto en el diseño académico) |

---

## Módulo 01 — Roles, Permisos, Usuarios y Autenticación

Revisado a fondo: `Controllers/Roles/RolesController.cs`, `Controllers/Usuarios/UsuariosController.cs`,
`Controllers/Account/AccountController.cs`, `Authorization/*`, `Services/Implementations/PermisoEvaluador.cs`,
`Data/DbInitializer.cs`, `Constants/Permisos.cs`, `Constants/PermisosCatalogo.cs`, `Constants/RolesBase.cs`,
`Views/Account/Login.cshtml`, contra `modulos/01-roles-permisos-usuarios.md`.

**Buenas noticias:** el sistema de Policies dinámicas, caché de permisos, lockout, sesión deslizante,
seed de roles/admin y protección de "último administrador"/roles fijos funcionan exactamente como
describe el documento — no se encontraron bugs de comportamiento en la lógica de autorización en sí.

### 1.1 — Conteos de permisos por defecto incorrectos en el doc del módulo (corrección menor)

- **Qué dice el doc** — `modulos/01-roles-permisos-usuarios.md`, tabla de §3: "Abogado/Asesor... 22
  permisos" y "Asistente... 6 permisos".
- **Qué hay en el código** — `Constants/RolesBase.cs` (`PermisosPorDefecto`): el arreglo de
  Abogado/Asesor tiene **25** códigos (Prospectos 5 + Clientes 4 + Propuestas 5 + Expedientes 9 +
  Facturacion 2), y el de Asistente tiene **9** códigos (Prospectos 4 + Clientes 2 + Facturacion 3).
  El listado detallado entre paréntesis que trae la misma fila del doc sí coincide exactamente con el
  código — solo el número resumen al inicio de cada fila está mal contado.
- **Por qué importa** — Bajo impacto (es solo un número en la documentación, no afecta al sistema),
  pero puede confundir a quien la use como checklist de cuántos permisos debería ver un rol recién
  creado.
- **Acción sugerida** — Corregir en `modulos/01-roles-permisos-usuarios.md` §3: "22 permisos" → "25
  permisos" (Abogado/Asesor) y "6 permisos" → "9 permisos" (Asistente).

### 1.2 — Catálogo de permisos creció (Calidad + Reportes ampliado) sin actualizar el doc del módulo

- **Qué dice el doc** — `modulos/01-roles-permisos-usuarios.md` §2: "`Constants/Permisos.cs`: 61
  códigos (`const string`) organizados en 10 clases anidadas por módulo (`Roles`, `Usuarios`,
  `Prospectos`, `Clientes`, `Servicios`, `Propuestas`, `Expedientes` [14 códigos, el más grande],
  `Facturacion`, `Reportes`, `Auditoria`)."
- **Qué hay en el código** — `Constants/Permisos.cs` tiene hoy **65 códigos en 11 clases**: a la lista
  del doc le falta la clase `Calidad` (2 códigos: `Ver`, `Registrar`), y `Reportes` creció de lo que
  tenía el doc a 7 códigos (incluye `Servicios` y `Rentabilidad`, que no estaban cuando se escribió el
  doc el 2026-09-18). `PermisosCatalogo.cs` está en sincronía exacta con `Permisos.cs` (11 módulos, sin
  duplicados, sin huérfanos) — no hay divergencia de código, solo el doc quedó desactualizado.
- **Por qué importa** — Es la contraparte exacta del hallazgo 2.1 de abajo: estos permisos nuevos
  existen porque **ya hay código real de Reportes y Evaluación de Calidad** (ver 2.1), algo que el
  README de `modulos/` todavía no refleja.
- **Acción sugerida** — Actualizar §2 del doc a "65 códigos en 11 clases" y agregar `Calidad` a la
  enumeración, una vez que existan `13-reportes.md`/`14-calidad.md` (ver 2.1).

### 1.3 — ✅ RESUELTO — Incoherencia #11 (`incoherencias_a_revisar.md`) ya está resuelta en el working tree actual, sin commitear

> **Resuelto el 2026-09-24** en `Controllers/Roles/RolesController.cs` y `Controllers/Usuarios/UsuariosController.cs`
> (branch `mt-002-transaccionalidad-auditoria`, **todavía sin commit** al momento de este QA). Detalle abajo.

- **Qué decía la incoherencia registrada** — "RF-001: Roles y Usuarios no generan ningún registro en
  la Bitácora de Auditoría" — `incoherencias_a_revisar.md` punto 11.
- **Qué hay en el código hoy (branch `mt-002-transaccionalidad-auditoria`, sin commit)** —
  `RolesController.cs` y `UsuariosController.cs` ahora llaman a `IBitacoraAuditoriaService.Registrar`
  en crear/editar/activar/desactivar rol y usuario, y en guardar permisos (registra la lista completa
  de permisos antes/después). Todo envuelto en `ITransaccionService.EjecutarIdentityAsync`, que además
  corrige un bug relacionado no documentado: antes, si fallaba `AddToRoleAsync` al crear un usuario, se
  hacía un `_userManager.DeleteAsync(nuevoUsuario)` manual como "rollback" (que a su vez podía fallar
  y dejar un usuario huérfano sin rol); ahora es una transacción real de base de datos. Documentado con
  detalle y con pruebas de fallo forzado en `docs/auditoria-transaccion.md`.
- **Por qué importa** — Es una mejora real, ya verificada en el código y bien probada, pero **todavía
  no forma parte de `master`** (son cambios sin commitear en este branch) y ni `incoherencias_a_revisar.md`
  ni `modulos/01-roles-permisos-usuarios.md` reflejan el cambio.
- **Acción sugerida** — Cuando se cierre MT-002: (a) marcar la incoherencia #11 como RESUELTA en
  `incoherencias_a_revisar.md` con referencia a `docs/auditoria-transaccion.md`; (b) actualizar
  `modulos/01-roles-permisos-usuarios.md` §9 punto 1 (ya no aplica) y el ítem del checklist §10 que dice
  "verificar que NINGUNA acción... aparece en la Bitácora" — el comportamiento esperado ahora es el
  opuesto (SÍ debe aparecer).

### 2.1 — [Ver también índice general] Módulos Reportes (RF-012) y Evaluación de Calidad (RF-013) ya tienen código real, pero no tienen doc de módulo

- Ver detalle en la sección "Hallazgos transversales" al final de este documento — afecta la
  planificación del resto de este QA (se agregan como módulos 13 y 14 a revisar).

### Verificado, sin hallazgos nuevos (ya conocido / correcto)

- Bug #3 del doc (`Login.cshtml` lee `?razon=desactivado` pero ningún controlador genera esa
  redirección) — **confirmado, sigue vigente**, no se repite como hallazgo nuevo.
- Lockout (5 intentos/15 min), sesión deslizante 1h, `SecurityStampValidationInterval` 1 min,
  "último administrador", protección de roles `EsFijo`, correo único, `PermisosCatalogo.Validar()`
  solo en Development — todo funciona tal como describe el doc.
- `PermisoPolicyProvider`/`PermisoAuthorizationHandler`/`PermisoEvaluador`: un código de permiso
  inexistente en `[Permiso("...")]` efectivamente bloquea a todos, incluido Administrador (política
  `RequireAssertion(_ => false)`).

## Módulo 02 — Catálogo Geográfico y Direcciones

Revisado a fondo: `Services/Implementations/ProspectoService.cs`, `ClienteService.cs` (creación,
edición y conversión de dirección), `Controllers/Prospectos/ProspectosController.cs` (endpoints de
cascada), `Views/Clientes/Editar.cshtml`, migración `20260909042953_AddDireccionesTriggers`, contra
`modulos/02-direcciones.md`.

**Sin hallazgos nuevos — todo lo documentado se confirmó exactamente igual en el código:**
- La asimetría de validación (solo "nacional sin distrito" tiene mensaje en español; "extranjero con
  distrito" solo lo bloquea el trigger SQL) está confirmada, pero además se confirmó que en la práctica
  es **inalcanzable por la UI normal**: tanto `ProspectoService` como `ClienteService` fuerzan
  `DistritoId = null` en código cuando `esNacional == false`, así que el trigger es puramente defensa
  en profundidad ante manipulación directa del request o de la BD — coincide con que el propio checklist
  del doc ya marca ese caso como "requiere manipular el request".
  `ClienteService.ConvertirDesdeProspecto` (línea 36-116) copia la `Direccion` del prospecto completa,
  incluido `TipoUbicacion`, sin re-derivar nada — confirma el punto 7 del doc, y además ya está envuelta
  en `ITransaccionService.EjecutarAsync` (consistente con `docs/auditoria-transaccion.md` §4.1).
- Los endpoints `ObtenerCantones`/`ObtenerDistritos` en `ProspectosController.cs:389-401` confirmados
  sin `[Permiso]` propio, solo `[Authorize]` de clase — accesibles por cualquier usuario autenticado.
- `Views/Clientes/Editar.cshtml` confirmado reutilizando el script y las URLs de Prospectos; no existe
  `Views/Clientes/Crear.cshtml` (consistente con que Clientes no tiene alta directa, ver módulo 04).
- Migración de triggers `20260909042953_AddDireccionesTriggers` existe y coincide con lo documentado.

## Módulo 03 — Prospectos y Seguimiento Comercial

Revisado: `Controllers/Prospectos/ProspectosController.cs` (todos los `[Permiso]` de sus acciones),
`Services/Implementations/ActividadSeguimientoService.cs`, `ProspectoService.cs`, contra
`modulos/03-prospectos.md`. Este doc ya venía muy completo — sus 3 discrepancias contra RF-003 (§7) se
confirmaron exactas en el código: `Prospectos.Calificar` no se usa en ningún `[Permiso(...)]` del
repo (solo se declara y se siembra por defecto); `RegistrarActividad`/`EditarActividad`/`EliminarActividad`
comparten el único permiso `Prospectos.Editar` sin comparar `UsuarioRegistroId`/`ResponsableId`;
`ConvertirACliente` solo exige `Prospectos.Convertir` (no hay chequeo adicional de rol Administrador);
correo duplicado se valida sin filtrar por `Estado` (incluye Descartados/Convertidos).

### 3.1 — Corrección menor al doc: dice que sus discrepancias no están registradas, pero sí lo están

- **Qué dice el doc** — `modulos/03-prospectos.md` §7, nota de cierre: "Estas 3 discrepancias...
  No se han corregido ni registrado todavía en `incoherencias_a_revisar.md`".
- **Qué hay realmente** — `incoherencias_a_revisar.md` puntos **#12** ("RF-003: matriz de permisos de
  actividades de seguimiento no implementada") y **#13** ("RF-003: la conversión de Prospecto a Cliente
  no está restringida al Administrador") son, casi palabra por palabra, las discrepancias 1 y 2 de esta
  misma sección. Sí están registradas — solo la discrepancia 3 (`Prospectos.Calificar` letra muerta) no
  tiene entrada propia allá (es menor, no contradice ningún RF).
- **Por qué importa** — Bajo impacto: alguien que confíe en esa nota podría no ir a revisar
  `incoherencias_a_revisar.md` pensando que el hallazgo es exclusivo de este doc.
- **Acción sugerida** — Actualizar la nota de cierre de §7 para referenciar los puntos #12 y #13 de
  `incoherencias_a_revisar.md` en vez de decir que no están registrados.

Sin más hallazgos nuevos — el resto del módulo (reglas de Editar/Descartar/Reactivar, bitácora,
notificación de actividad próxima) se comporta tal como describe el doc.

## Módulo 04 — Clientes

Revisado: `Controllers/Clientes/ClientesController.cs` (todos los `[Permiso]`), `Services/Implementations/ClienteService.cs`
completo (`Listar`, `ReasignarResponsable`, `Editar`), y confirmado uso de `Clientes.Crear` en todo el
repo, contra `modulos/04-clientes.md`.

**Sin hallazgos nuevos.** Confirmado exacto: no existe acción `Crear` ni endpoint alguno para alta
directa; `Clientes.Crear` no se usa en ningún `[Permiso(...)]` del repo (permiso huérfano); `Listar`
(`ClienteService.cs:118-145`) no filtra por `ResponsableId` — cualquiera con `clientes.ver` ve todos
los clientes (mismo patrón que tenía Expedientes antes de HU-017/021, ya registrado como incoherencia
#7 — pero para Clientes nunca se corrigió); `ReasignarResponsable` (`ClienteService.cs:347-372`) en
efecto solo toca `Cliente.ResponsableId`, nada más — confirma incoherencia #8 tal cual está registrada.
`ClienteService.ConvertirDesdeProspecto` ya está envuelta en `ITransaccionService` (MT-002); el resto
de operaciones de Clientes (Editar/Desactivar/Reactivar/ReasignarResponsable) siguen sin transacción,
consistente con el alcance explícito documentado en `docs/auditoria-transaccion.md` §4.5.

## Módulo 05 — Catálogo de Servicios

Revisado: `Services/Implementations/ServicioService.cs` completo (`Crear`, `Editar`, `Desactivar`),
migraciones de `CatalogoServicios`, contra `modulos/05-servicios.md`.

**Sin hallazgos nuevos.** Confirmado exacto: `Editar` (línea 110-142) captura `valorAnterior`/`valorNuevo`
solo de `servicio.Nombre` aunque también reescribe `AreaCategoria`/`Descripcion`/`PrecioBase`/`TipoServicio`
— confirma el bug #3 del propio doc (auditoría incompleta). No hay validación de nombre duplicado en
el servicio ni índice único en las migraciones — confirma bug #1. `Desactivar` no valida uso en
propuestas — confirma bug #2 (documentado como comportamiento intencional).

## Módulo 06 — Propuestas

Revisado: `Services/Implementations/PropuestaService.cs` completo (`Crear`, `Editar`,
`MarcarComoEnviada/Aceptada/Rechazada`, `ValidarDestinatarioActivo`), contra `modulos/06-propuestas.md`.

**Sin hallazgos nuevos — doc excelente, cada afirmación se verificó exacta:**
- Bug de zona horaria **confirmado, sigue presente**: `PropuestaService.cs:556`,
  `PlazoComprometido = DateTime.UtcNow.Date.AddDays(propuesta.PlazoDias)`.
- `ValidarDestinatarioActivo` se invoca en `MarcarComoEnviada` (línea 486) y `MarcarComoAceptadaInterno`
  (línea 525), pero **no** en `MarcarComoRechazada` (línea 592-622) — confirma la asimetría documentada.
- Al aceptar una propuesta de Prospecto, el responsable del expediente/cliente nuevo es
  `propuesta.ElaboradaPorId` (línea 542/545), no el usuario que ejecuta la acción — confirmado.
- `MarcarComoAceptada` ya está envuelta en `ITransaccionService.EjecutarAsync` (línea 511-512),
  consistente con `docs/auditoria-transaccion.md` §4.1.

> **Ver T.3** (sección de hallazgos transversales, arriba): el doc de este módulo no menciona la
> validación de "solicitud pro bono aprobada y disponible" que ahora exige `PropuestaService` al
> marcar/editar una propuesta con `ModalidadPago = ProBono` — hallazgo nuevo detectado al revisar el
> módulo 12, no repetido aquí en detalle.

## Módulo 07 — Expedientes, Tareas, Entregables y Revisiones (Kanban)

Es el módulo más grande del sistema (`ExpedienteService.cs` tiene ~30 métodos públicos/privados);
revisión por muestreo dirigido a las afirmaciones de mayor riesgo del doc en vez de línea por línea:
filtro de cartera, cierre de expediente, transaccionalidad (MT-002), bug visual de zona horaria y
permisos huérfanos, contra `modulos/07-expedientes-kanban.md`.

**Sin hallazgos nuevos — todas las afirmaciones muestreadas se confirmaron exactas:**
- Filtro de cartera (`ExpedienteService.cs:132-133` y `:170-171`): `ResponsableId == usuarioActualId
  || Tareas.Any(ColaboradorResponsableId == usuarioActualId)` — confirmado igual en `Listar` y
  `ObtenerDetalle`.
- `CerrarExpedienteInterno` (línea 511-529) confirma exactamente la regla estricta documentada
  (exige ≥1 tarea y **todas** `Aprobada`, incluida `Devuelta` como bloqueante) — y de paso el código trae
  comentarios propios explicando el porqué de esa decisión y de otra no documentada en este módulo (la
  modalidad/monto de la factura se toma de `Propuesta.ModalidadPago`, no de `Cliente.ModalidadPago`,
  decisión ya resuelta el 2026-09-18 — relevante para el QA del módulo 08).
- Bug visual de zona horaria en "días restantes" **confirmado, sigue presente**:
  `Views/Expedientes/Index.cshtml:49` y `Detalle.cshtml:32` usan `DateTime.Today` (hora local del
  servidor) contra un `PlazoComprometido` calculado con `DateTime.UtcNow.Date`.
  `Views/Expedientes/Detalle.cshtml:157` tiene el mismo patrón (`DateTime.Today`) para marcar tareas
  vencidas — no estaba explícitamente mencionado en el doc pero es el mismo bug, mismo efecto (+1 día
  visual en ciertas horas), no se registra aparte por ser la misma causa raíz ya documentada.
- `Expedientes.Asignar` confirmado huérfano: declarado y asignado por defecto (`RolesBase.cs:32`) pero
  no aparece en ningún `[Permiso(...)]` del repo — mismo patrón que `Prospectos.Calificar` (módulo 03)
  y `Clientes.Crear` (módulo 04); no se abre hallazgo nuevo por repetirse el patrón, pero queda anotado
  como observación transversal (ver T.2 abajo).
- `CerrarExpediente`, `EliminarTarea`, `AgregarArchivoEntregable`, `EliminarArchivoEntregable` y
  `RevisarEntregable` (Aprobar/Devolver) confirmados envueltos en `ITransaccionService.EjecutarAsync`,
  y la notificación tras revisar un entregable confirmada **fuera** de la transacción (después del
  commit) — consistente con `docs/auditoria-transaccion.md` §4.4/§4.5.

## Módulo 08 — Facturación

Revisado a fondo: `Controllers/Facturas/FacturasController.cs` (8 acciones), `Services/Implementations/FacturaService.cs`
completo (487 líneas: `Listar`, `ObtenerDetalle`, `RegistrarAbono`, `ObtenerEstadoCuenta`, `AnularFactura`,
`AnularAbono`), y el fragmento de `ExpedienteService.CerrarExpedienteInterno` que genera la factura,
contra `modulos/08-facturacion.md`.

### 8.1 — El doc contradice al código en la fuente de `ModalidadPago` de la factura (y el propio código explica por qué cambió)

- **Qué dice el doc** — `modulos/08-facturacion.md` §2 y §4: "`ModalidadPago`... Copiada de
  `Cliente.ModalidadPago` al momento del cierre, **no** de `Propuesta.ModalidadPago`" y "decisión
  explícita porque RF-010 dice literal 'modalidad de pago correspondiente al cliente'".
- **Qué hace el código hoy** — `ExpedienteService.cs:534-553` (método `CerrarExpedienteInterno`) usa
  **`expediente.Propuesta.ModalidadPago`**, no `Cliente.ModalidadPago`, tanto para decidir si es pro
  bono (`esProBono = expediente.Propuesta.ModalidadPago == ModalidadPago.ProBono`) como para el campo
  `Factura.ModalidadPago` en sí. El propio código trae un comentario fechado explicando el cambio:
  *"RF-007/RF-011 (rediseñado 2026-09-18)... usar `Cliente.ModalidadPago`... generaba facturas con
  monto real pero etiquetadas 'Pro bono' cuando el cliente había quedado con esa modalidad por un caso
  anterior sin relación — contradictorio y confirmado en pruebas."*
- **Por qué importa** — Es el caso exacto que motivó el cambio: dos documentos (el código y el doc del
  módulo) fechados el mismo día (2026-09-18) describen decisiones **opuestas** sobre la misma regla de
  negocio. El código es más reciente/autoritativo según la jerarquía del router del proyecto ("el
  código manda en qué está realmente construido hoy"), pero cualquiera que lea el doc del módulo antes
  que el código llegaría a la conclusión contraria y probaría/documentaría el criterio equivocado.
- **Acción sugerida** — Reescribir `modulos/08-facturacion.md` §2 y §4 para reflejar `Propuesta.ModalidadPago`
  como fuente real, citando el razonamiento que ya está en el comentario de `ExpedienteService.cs:534-545`.
  Esto es, de hecho, una sub-parte de 8.2 (el doc necesita reescritura completa).

### 8.2 — Confirmado: el doc está completamente obsoleto, tal como ya advertía el README (HU-022/023/024 sí implementadas)

- **Qué dice el doc** — Todo el documento describe el módulo como "Parcialmente implementado. Solo
  cubre la emisión automática... Registrar abonos, ver estado de cuenta y anular factura son funciones
  futuras", con HU-022/023/024 marcadas "NO están implementadas todavía".
- **Qué hay en el código hoy** — `FacturasController.cs` tiene 8 acciones reales, no 2: `Index`,
  `Detalle`, `RegistrarAbono` (GET+POST), `EstadoCuenta`, `RegistrarAbonoAjax`, `Anular`, `AnularAbono`,
  `DescargarComprobante`. `FacturaService.cs` implementa las 4 (`RegistrarAbono`, `ObtenerEstadoCuenta`,
  `AnularFactura`, `AnularAbono`) con reglas de negocio completas y bien pensadas: abono no puede
  superar el saldo pendiente, recalcula el monto pagado post-insert para evitar condiciones de carrera
  entre abonos concurrentes, sube comprobante con `AlRevertir` registrado *antes* de escribir el archivo
  (consistente con `docs/auditoria-transaccion.md` §4.2), no permite anular una factura `Pagada`, no
  revive una factura anulada al anular uno de sus abonos, y separa "anular abono" de "anular factura"
  exactamente como describe la "Acción tomada" de la incoherencia #14 de `incoherencias_a_revisar.md`
  (con el mismo razonamiento en el comentario de `FacturaService.cs:424-427`).
- **Por qué importa** — Ya estaba anotado en el propio `README.md` de `modulos/` ("⚠️ Doc desactualizado...
  Pendiente de reescribir"), así que esto **confirma y detalla** esa advertencia en vez de ser un
  hallazgo nuevo — se registra aquí para que quien reescriba el doc tenga ya mapeado qué cubrir.
- **Acción sugerida** — Reescribir `modulos/08-facturacion.md` completo siguiendo el mismo formato que
  los demás módulos, cubriendo las 4 acciones nuevas, sus permisos (`facturacion.abono` = registrar
  abonos y ver todas las facturas sin filtro de cartera; `facturacion.anular` = un nivel más estricto,
  solo Administrador por defecto — confirmado en `RolesBase.cs`, ningún rol no-fijo tiene `Anular`), y
  el punto 8.1 de arriba sobre la fuente real de `ModalidadPago`.

**Sin más hallazgos** — filtro de cartera por `Expediente.ResponsableId` (no `Cliente.ResponsableId`)
confirmado igual en `Listar`/`ObtenerDetalle`/`ObtenerEstadoCuenta`; comprobante limitado a PDF/imagen
≤10MB; `RegistrarAbono`/`AnularFactura`/`AnularAbono` ya transaccionales (`ITransaccionService`).

> **Ver T.3**: el hallazgo 8.1 de arriba (fuente real de `ModalidadPago`) es en realidad una pieza de
> un rediseño más grande del mecanismo Pro Bono que también toca los módulos 06 y 12 — detalle completo
> en T.3, sección de hallazgos transversales.

## Módulo 09 — Notificaciones

Revisado: `Services/Implementations/RevisionVencimientosBackgroundService.cs` completo,
`NotificacionService.cs` completo, contra `modulos/09-notificaciones.md`.

**Sin hallazgos nuevos — doc muy preciso, confirmado exacto:** `hoy = DateTime.UtcNow.Date` (línea 49,
sin el bug de zona horaria de la vista de Expedientes); los avisos "próximo/vencido" y "escalamiento"
usan `if` independientes (no `else if`), así que coexisten tal como documenta el doc; umbrales de 3
días (tarea) y 5 días (expediente) y 1 día (escalamiento) confirmados; `AlertaExpedienteProximo` solo
al responsable, sin supervisores, confirmado (única llamada sin loop sobre `supervisores`);
`NotificarSiNoExiste` deduplica por `UsuarioId+Tipo+EntidadRelacionadaTipo+EntidadRelacionadaId` sin
ventana de tiempo, y `Ir`/`Eliminar` validan pertenencia (`UsuarioId != usuarioId` → excepción) tal
como describe el doc.

## Módulo 10 — Dashboard

Revisado: `Services/Implementations/DashboardService.cs` completo (16 líneas de lógica) y
`Models/DTOs/DashboardDTO.cs`, contra `modulos/10-dashboard.md`.

**Sin hallazgos nuevos.** Confirmado exacto: consulta directa a `Tareas` filtrando por
`ColaboradorResponsableId == usuarioId` (no lee `Notificacion` para esta parte); `TareasVencidas`
(`FechaLimite.Date < DateTime.Today`) y `TareasPorVencer` (`>= DateTime.Today`) son propiedades
calculadas mutuamente excluyentes que en efecto suman `TareasProximas.Count` — confirma que la
tarjeta "activas por vencer" es el total, no un tercer conjunto independiente, tal como advierte el
doc en su punto 1 de "Pendientes y notas".

## Módulo 11 — Bitácora de Auditoría

Revisado: `Controllers/Auditoria/AuditoriaController.cs`, `Services/Implementations/BitacoraAuditoriaService.cs`
completo, `Services/Implementations/ExpedienteService.cs`/`ActividadSeguimientoService.cs` (nombres de
módulo usados en `Registrar(...)`), migraciones de `BitacoraAuditoria`, contra `modulos/11-auditoria.md`
(doc fechado hoy mismo, 2026-09-24 — el más actualizado de todos).

### 11.1 — ✅ RESUELTO — Incoherencia #9 (`incoherencias_a_revisar.md`): ya existe pantalla de consulta de la bitácora, y ya está commiteada

> **Resuelto y commiteado** en `5e418f9` ("feat: Add audit log query (HU-034)"), **2026-09-19** — a
> diferencia del hallazgo 1.3 (módulo 01), este no está pendiente de commit, ya es parte del historial.

- **Qué decía la incoherencia registrada** — "No existe ningún `Controllers/BitacoraAuditoria/BitacoraAuditoriaController.cs`
  ni ninguna vista... nadie, ni el Administrador, puede ver la bitácora desde la interfaz."
- **Qué hay en el código hoy** — `Controllers/Auditoria/AuditoriaController.cs` (`Index`/`Detalle`,
  ambos protegidos con `[Permiso(Permisos.Auditoria.Ver)]`), `Views/Auditoria/Index.cshtml` y
  `Detalle.cshtml`, con filtros por usuario/tipo de acción/rango de fecha-hora, paginación de a 25, y
  (agregado hoy mismo, sin commitear todavía) una columna "Entidad" que resuelve el nombre legible del
  registro afectado en vez de mostrar solo el id crudo.
- **Acción sugerida** — Marcar la incoherencia #9 como RESUELTA en `incoherencias_a_revisar.md`,
  referenciando el commit `5e418f9` y `modulos/11-auditoria.md`.

### 11.2 — ✅ RESUELTO — Incoherencia #10 (`incoherencias_a_revisar.md`): ya existe el trigger de inmutabilidad, y ya está commiteado

> **Resuelto y commiteado** junto con 11.1, mismo commit `5e418f9`, migración
> `Migrations/20260919074043_AddBitacoraAuditoriaInmutableTrigger.cs`.

- **Qué decía la incoherencia registrada** — "La única migración que toca esta tabla... solo crea la
  tabla... no crea ningún trigger... nada a nivel de base de datos lo impediría [un UPDATE/DELETE]."
- **Qué hay en el código hoy** — Migración `20260919074043_AddBitacoraAuditoriaInmutableTrigger.cs`
  crea `TR_BitacoraAuditoria_Inmutable` (`INSTEAD OF UPDATE, DELETE`), verificado con SQL directo
  (documentado en `modulos/11-auditoria.md` §5): un `UPDATE`/`DELETE` directo sobre la tabla falla con
  mensaje explícito, incluso sin pasar por la aplicación.
- **Acción sugerida** — Marcar la incoherencia #10 como RESUELTA en `incoherencias_a_revisar.md`, misma
  referencia que 11.1.

**Sin más hallazgos** — confirmado exacto: `Registrar()` solo agrega al `DbContext` (no guarda);
`ExpedienteService` ya escribe `"Tareas"` para todo lo relacionado a una tarea puntual y reserva
`"Expedientes"` solo para `ReasignarResponsable`/`CerrarExpediente` (por `ExpedienteId`);
`ActividadSeguimientoService` ya escribe `"Actividades"` en vez de `"Prospectos"`; el corte de
desambiguación en `BitacoraAuditoriaId > 20389` existe tal cual en `BitacoraAuditoriaService.cs`;
`AuditoriaController` protegido con `auditoria.ver` en ambas acciones; validación "Desde posterior a
Hasta" limpia ambos filtros con aviso, sin romper la pantalla — todo tal como describe el doc.

**Nota:** las columnas "Entidad"/resolución de nombre y la optimización de paginación (ids primero)
que describe §4 del doc **están en el working tree sin commitear** (mismo branch `mt-002-transaccionalidad-auditoria`,
archivos `BitacoraDetalleDTO.cs`, `BitacoraRegistroDTO.cs`, `Views/Auditoria/*`, `wwwroot/css/auditoria.css`,
`wwwroot/js/auditoria.js` en el `git status`) — a diferencia de 11.1/11.2 (ya commiteados desde
2026-09-19), esta parte todavía no forma parte de `master`.

## Módulo 12 — Solicitudes Pro Bono

Revisado: `Services/Implementations/ProBonoService.cs` completo, y `PropuestaService.ValidarYReservarSolicitudProBono`
(el enganche con Propuestas), contra `modulos/12-probono.md`. Doc ya venía con su propio checklist
mayormente probado en vivo (marcado `[x]`).

**Hallazgo importante: ver T.3** en la sección de hallazgos transversales — el §4/§5 de este doc describe
un mecanismo (`Cliente.ModalidadPago = ProBono` automático al aprobar) que **ya no existe**: fue
rediseñado el mismo 2026-09-18 hacia un sistema de reserva por Propuesta (`SolicitudProBono.PropuestaConsumidaId`).
El propio `ProBonoService.ResolverInterno` trae el comentario explicando el cambio (línea 219-227).

**Confirmado sin cambios, resto del módulo:** `PuedeVerTodasAsync` usa `Facturacion.AprobarProbono`
(no `Facturacion.Abono`, correcto — es un permiso distinto al de Facturación) para decidir "ve todas"
vs. "solo las propias" (`Listar`, línea 61-75); `ResolverInterno` bloquea una solicitud ya resuelta
(`Decision != Pendiente"`, línea 208-209) — todo tal como documenta el doc.

**Nota menor sobre el checklist del propio doc:** el último ítem (sin marcar) dice "no hay pantalla de
consulta todavía [de auditoría], ver `11-auditoria.md`" — ya desactualizado, la pantalla existe y está
commiteada desde el 2026-09-19 (ver 11.1 arriba); se puede marcar y verificar directo en `/Auditoria/Index`
filtrando por módulo "ProBono" en vez de ir a la base de datos.

## Módulo 13 — Reportes y Analítica (RF-012)

**No tiene doc de módulo previo** (ver T.1) — este es un QA de primera pasada directo contra el código
y el enunciado, no una verificación contra un doc existente. Recomendación general: crear
`modulos/13-reportes.md` con este mismo contenido como punto de partida.

Revisado: `Controllers/Reportes/ReportesController.cs` completo, `Services/Implementations/ReporteService.cs`
completo (532 líneas, 7 reportes), permisos y roles por defecto.

**Hallazgos:**
- Los 7 reportes están completos y cableados: `Conversion` (propuestas), `ConversionProspectos`,
  `IngresosPorServicio`, `DistribucionGeografica`, `CargaTrabajo`, `ComparativoServicios`, `Rentabilidad`
  — cada uno protegido por su propio permiso (`Reportes.Conversion/Ingresos/Geo/Carga/Servicios/Rentabilidad`,
  con `Conversion` cubriendo dos acciones). `Index` no exige un permiso fijo: recorre los 7 y entra si
  el usuario tiene al menos uno — diseño correcto para que un rol con acceso parcial a reportes no
  quede totalmente bloqueado del hub.
- Ninguno de los 4 roles no-fijos tiene ningún permiso `Reportes.*` por defecto (`RolesBase.cs`) — hoy
  son, en la práctica, exclusivos de Administrador (coherente con que sean reportes gerenciales), pero
  el catálogo permite asignarlos a otro rol si se necesita.
- `ResolverRango` (línea 517-530) usa `DateTime.UtcNow.Date` de forma consistente para los períodos
  por defecto — **no** repite el bug de zona horaria de las vistas de Expedientes/Dashboard.
- Filtrado de rango de fechas invertido (`Desde` > `Hasta`) se descarta con aviso y cae al período por
  defecto — mismo patrón ya usado en Auditoría y Facturación, consistente.
- No se filtra por cartera/responsable en ningún reporte — es una decisión razonable dado que son datos
  agregados de firma completa y ya están restringidos a quien tenga el permiso puntual (típicamente
  Administrador), no se registra como hallazgo.

**Sin bugs encontrados** en la muestra revisada — el nivel de calidad es consistente con el resto del
sistema (comentarios explicando decisiones de negocio, manejo de rango de fechas idéntico al resto de
la app, sin bug de zona horaria).

## Módulo 14 — Evaluación de Calidad (RF-013)

**No tiene doc de módulo previo** (ver T.1). Recomendación general: crear `modulos/14-calidad.md`.

Revisado: `Controllers/Calidad/CalidadController.cs` completo, `Services/Implementations/EvaluacionCalidadService.cs`,
migración `20260919061810_AddEvaluacionesCalidad.cs`, contra el enunciado del TFG (RF-013) y el diseño
académico original de `EvaluacionesCalidad` (`hikari_legal_documentacion_bd.md`, que sí documenta un
trigger `TR_EvaluacionesCalidad_Inmutable`).

### 14.1 — Falta el trigger de inmutabilidad que el propio diseño académico prevé para esta tabla

- **Qué dice el diseño documentado** — La sección "4. Triggers de integridad de negocio" del diseño
  académico (`hikari_legal_documentacion_bd.md` / PDF del proyecto) incluye explícitamente:
  `TR_EvaluacionesCalidad_Inmutable | EvaluacionesCalidad | INSTEAD OF UPDATE, DELETE | Bloquea toda
  modificación o eliminación de una evaluación ya registrada` — mismo patrón que
  `TR_BitacoraAuditoria_Inmutable` (ese sí implementado, ver hallazgo 11.2).
- **Qué hay en el código hoy** — La migración `20260919061810_AddEvaluacionesCalidad.cs` solo crea la
  tabla y el CHECK `CK_EvaluacionCalidad_Puntuacion` (`Puntuacion BETWEEN 1 AND 5`). No hay ningún
  trigger `INSTEAD OF UPDATE, DELETE`, ni índice único sobre `ExpedienteId`. La inmutabilidad y la
  regla "una evaluación por expediente" hoy dependen **solo** de que `CalidadController` no tenga
  acciones `Editar`/`Eliminar`, y de que `EvaluacionCalidadService.Registrar` (línea 136, 143-151)
  rechace un segundo registro para el mismo `ExpedienteId` — ambas son capas de aplicación, no de base
  de datos.
- **Por qué importa** — Es exactamente el mismo patrón que ya se identificó y corrigió para la Bitácora
  de Auditoría (incoherencia #10, resuelta el 2026-09-19 — ver hallazgo 11.2 arriba): sin el trigger,
  un `UPDATE`/`DELETE` directo por SQL (o un descuido futuro en código, por ejemplo un script de
  limpieza de datos) podría alterar o borrar una evaluación de calidad ya registrada, algo que tanto el
  diseño académico como el propio código de otros módulos (`FacturaService.cs`, comentario línea
  424-427: "igual criterio que Bitácora de Auditoría y Evaluaciones de Calidad (inmutables)") asumen
  que no puede pasar.
- **Acción sugerida** — Agregar una migración nueva con `TR_EvaluacionesCalidad_Inmutable`
  (`INSTEAD OF UPDATE, DELETE`), siguiendo el mismo patrón que
  `20260919074043_AddBitacoraAuditoriaInmutableTrigger.cs`. Opcionalmente, agregar también un índice
  único sobre `ExpedienteId` como segunda capa para "una evaluación por expediente" (hoy solo en C#).

**Sin más hallazgos** — confirmado: solo expedientes `Cerrado` son evaluables
(`ObtenerExpedienteEvaluable`); `Registrar` rechaza un segundo intento sobre el mismo expediente con
mensaje claro; `Puntuacion` restringida 1-5 tanto en CHECK de BD como en DTO; sin acciones de
edición/eliminación expuestas en el controller, consistente con "evaluación inmutable" como intención
de diseño (aunque, ver 14.1, todavía no reforzada a nivel de BD).
