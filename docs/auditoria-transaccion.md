# Auditoría y transaccionalidad (MT-002)

> **Tarea técnica:** MT-002, issue #50 · **Requisitos:** RNF-006 (integridad de datos) y RNF-002 (trazabilidad) · **Rama:** `mt-002-transaccionalidad-auditoria`

Este documento explica **qué problema había, cómo funciona la solución, dónde se aplicó y cómo se probó**.

## 1. Resumen en una frase

Toda operación crítica se ejecuta dentro de **una sola transacción de base de datos** que incluye el cambio de negocio **y** su registro de auditoría: o se guardan los dos, o no se guarda ninguno.

## 2. El problema

### 2.1 Cómo se guardaba antes

Cada servicio hacía varios `SaveChangesAsync()` seguidos, sin ninguna transacción que los uniera. Cada uno era una transacción independiente:

```mermaid
sequenceDiagram
    participant S as Servicio
    participant DB as Base de datos
    S->>DB: SaveChanges 1 (el cambio de negocio)
    Note over DB: Confirmado. Ya no se puede deshacer
    S->>S: Registrar(auditoría), solo agrega al contexto
    S->>DB: SaveChanges 2 (la bitácora)
    Note over S,DB: Si algo falla aquí, el cambio ya quedó guardado sin su registro
```

### 2.2 Qué consecuencias tenía

| Caso | Qué podía quedar a medias |
|---|---|
| Aceptar una propuesta de un prospecto | Cliente creado y prospecto convertido, pero la propuesta seguía "enviada" y sin expediente. Reintentar daba "el prospecto ya no está activo" y no había forma de recuperarlo desde la interfaz. |
| Guardar permisos de un rol | Cada permiso se guardaba solo. Un fallo dejaba el rol con permisos que nadie eligió. |
| Crear usuario | Se creaba el usuario y luego el rol; si el rol fallaba se hacía un `DeleteAsync` manual (que también podía fallar). |
| Editar usuario | Datos ya actualizados pero rol sin cambiar, o usuario sin ningún rol. |
| Abono con comprobante | Registro creado sin archivo, o archivo en disco sin registro. |
| Cualquier cambio + bitácora | Cambio guardado sin su registro de auditoría (viola RNF-002). |

### 2.3 Hallazgo adicional: auditorías que se perdían en silencio

`IBitacoraAuditoriaService.Registrar` **solo agrega la fila al contexto; no guarda**. Tres métodos de `ExpedienteService` llamaban a `Registrar` como última instrucción y **nunca guardaban después**, así que su registro de auditoría jamás llegaba a la base:

- `AgregarArchivoEntregable` (adjuntar archivos)
- `EliminarArchivoEntregable`
- `EliminarTarea`

Se comprobó con datos reales: había **15 archivos de entregables adjuntados y 0 registros** de auditoría de esa acción. La solución de este documento lo corrige sin tocar esas llamadas (ver §3.3).

## 3. La solución

### 3.1 Ideas clave

1. **Un solo `DbContext` por petición.** `UserManager` y `RoleManager` de Identity usan ese mismo contexto. Una transacción sobre él cubre servicios de negocio **e** Identity.
2. **Una transacción reentrante** (`ITransaccionService`). Si ya hay una abierta, la operación se une a ella; si no, abre una. Así una operación puede llamar a otra (aceptar propuesta → convertir prospecto) y todo queda en la misma transacción.
3. **Un guardado final de seguridad.** Antes de confirmar, la transacción ejecuta un último `SaveChangesAsync()`. Cualquier `Registrar` pendiente se guarda ahí, dentro de la misma transacción.
4. **Acciones diferidas** para lo que no es de base de datos (archivos en disco): se ejecutan solo cuando se sabe si la transacción se confirmó o se revirtió.

### 3.2 Flujo de `EjecutarAsync`

Archivos: [ITransaccionService.cs](../Services/Interfaces/ITransaccionService.cs) y [TransaccionService.cs](../Services/Implementations/TransaccionService.cs).

```mermaid
flowchart TD
    A["EjecutarAsync(operacion)"] --> B{"¿Ya hay una transacción abierta?"}
    B -->|Sí, es una llamada anidada| C["Ejecuta la operación directamente<br/>y devuelve el resultado.<br/>La transacción exterior decide el final"]
    B -->|No, es la exterior| D["BeginTransaction"]
    D --> E["Ejecuta la operación<br/>(los servicios hacen sus SaveChanges<br/>y sus Registrar)"]
    E --> F{"¿Lanzó una excepción?"}
    F -->|No| G["SaveChanges final<br/>(guarda Registrar pendientes)"]
    G --> H["Commit"]
    H --> I["Ejecuta las acciones AlConfirmar<br/>(ej. borrar archivos de una tarea eliminada)"]
    I --> J(["Devuelve el resultado"])
    F -->|Sí| K["Rollback<br/>+ ChangeTracker.Clear"]
    K --> L["Ejecuta las acciones AlRevertir<br/>(ej. borrar el archivo que se acababa de escribir)"]
    L --> M(["Relanza la excepción"])
    G -.->|falla el guardado final| K
    H -.->|falla el commit| K
```

Detalles importantes:

- **Cualquier** excepción revierte, incluida `ReglaNegocioException` (una validación que falla a mitad de camino también deshace lo ya guardado).
- Las acciones diferidas **nunca lanzan hacia afuera**: si borrar un archivo falla, se registra en el log pero no cambia el resultado de la operación.
- `AlConfirmar` / `AlRevertir` solo se pueden registrar **dentro** de `EjecutarAsync`; fuera lanzan `InvalidOperationException`.
- `EnableRetryOnFailure` ya está activo (ver §3.5): todo el ciclo abrir transacción, operación, guardado final y commit se ejecuta dentro de una estrategia de ejecución de EF, y este único archivo es el que lo adapta.

### 3.3 Por qué también arregla las auditorías perdidas

```mermaid
sequenceDiagram
    participant S as ExpedienteService.EliminarTarea
    participant T as TransaccionService
    participant DB as Base de datos
    T->>DB: BEGIN TRANSACTION
    T->>S: EliminarTareaInterno()
    S->>DB: SaveChanges (borra la tarea)
    S->>S: Registrar(eliminar), solo agrega al contexto
    S-->>T: termina, sin guardar
    T->>DB: SaveChanges final, ahora sí guarda la bitácora
    T->>DB: COMMIT
```

### 3.4 Cómo se aplica en un servicio

El método público conserva su firma y delega en uno privado con la lógica original. Casi no se toca el código existente:

```csharp
public async Task<int> MarcarComoAceptada(int id, string usuarioActualId)
    => await _transaccionService.EjecutarAsync(() => MarcarComoAceptadaInterno(id, usuarioActualId));

private async Task<int> MarcarComoAceptadaInterno(int id, string usuarioActualId)
{
    // ...la lógica de siempre, con sus SaveChanges y sus Registrar...
}
```

### 3.5 Reintentos ante errores transitorios (despliegue en Azure SQL serverless)

**Motivo:** Azure SQL Database en nivel serverless se pausa solo. Mientras se reanuda (hasta ~60 s) la primera conexión falla con errores transitorios como el 40613. Para que la aplicación se recupere sin mostrar error, `Program.cs` activa `EnableRetryOnFailure(maxRetryCount: 6, maxRetryDelay: 30 s)` sobre `UseSqlServer`.

**Problema:** una estrategia con reintentos **no admite** transacciones abiertas a mano con `BeginTransactionAsync` fuera de ella (EF lanza `InvalidOperationException`). `TransaccionService` es el único lugar del proyecto que las abre, así que es el único archivo que cambia: la firma de las dos sobrecargas de `EjecutarAsync` y todos sus llamadores quedan igual.

```mermaid
flowchart TD
    A["EjecutarAsync(operacion)"] --> B{"¿Ya hay una transacción abierta?"}
    B -->|Sí| C["Ejecuta la operación directamente (anidada)"]
    B -->|No| S["Captura el estado de las entidades que el llamador<br/>ya tenía cargadas en el DbContext"]
    S --> E["CreateExecutionStrategy().ExecuteAsync"]
    E --> I["Intento N: BeginTransaction → operación → SaveChanges final → Commit"]
    I -->|éxito| OK["Ejecuta AlConfirmar UNA vez<br/>y devuelve el resultado"]
    I -->|falla| F["Rollback<br/>+ restaura el estado previo del tracker<br/>+ ejecuta los AlRevertir de ESE intento y los vacía"]
    F --> T{"¿Error transitorio<br/>y quedan reintentos?"}
    T -->|Sí, se registra un warning| I
    T -->|No| X["ChangeTracker.Clear y relanza la excepción"]
```

**Acciones diferidas (`AlConfirmar` / `AlRevertir`).** Se registran **dentro** de la operación (`ExigirTransaccion` lo obliga), por lo que cada intento registra las suyas desde cero y se vacían al terminar cada intento:

- `AlConfirmar`: solo se ejecutan tras el commit exitoso del intento final. Nunca en intentos intermedios.
- `AlRevertir`: limpian efectos en disco **de su propio intento** (la carpeta del abono, el archivo de la revisión). Por eso se ejecutan al final de cada intento fallido, una vez cada una: si se pospusieran hasta el final, el reintento escribiría en otra carpeta (el abono recibe un `AbonoId` nuevo) y la del intento fallido quedaría huérfana. Ninguna envía correos ni escribe en la bitácora; esas escrituras son filas dentro de la transacción y se revierten con ella.
- Las notificaciones y correos siguen enviándose **fuera** de `EjecutarAsync`, después del commit, así que un reintento no puede duplicarlos.

**Change tracker.** Si un intento falla después de agregar entidades, el reintento volvería a agregarlas y se duplicarían. La solución obvia, `ChangeTracker.Clear()` entre intentos, es incorrecta aquí: los controllers de Usuarios y Roles cargan y modifican entidades de Identity **antes** de llamar a `EjecutarIdentityAsync` (`usuario.Activo = false`, `usuario.NombreCompleto = ...`) y la lambda las reutiliza. Se probó: sin restaurar su estado, el reintento falla con `ConcurrencyFailure`, porque tras el primer `SaveChanges` (revertido en la base) el `ConcurrencyStamp` original del tracker ya no coincide con el de la fila. Por eso, al fallar un intento:

1. Se desvinculan las entidades que el intento cargó o agregó (la operación las vuelve a cargar o crear).
2. Las que el llamador ya tenía cargadas vuelven a su estado exacto de antes del primer intento (valores originales, valores actuales y estado).

Solo si el error es definitivo (no transitorio, o se agotaron los reintentos) se hace `ChangeTracker.Clear()`, como antes.

**Registro.** Cada reintento deja un `LogWarning` ("Reintentando la transacción (intento N)...") con la excepción del intento anterior. Si se agotan los reintentos EF lanza `RetryLimitExceededException` (la excepción original queda en `InnerException`); los errores no transitorios, como `ReglaNegocioException`, se relanzan tal cual y sin reintentar.

Los errores de negocio (`ReglaNegocioException` y `TransaccionFallidaException`) nunca son transitorios, así que se atrapan dentro del intento, se hace el rollback y se relanzan **fuera** de la estrategia de EF con `ExceptionDispatchInfo`. Sin esto el comportamiento en producción es el mismo, pero con el depurador de Visual Studio la excepción atraviesa código de EF (que no es "mi código") y el depurador se detiene como "excepción no controlada por el usuario" aunque el controller la capture y muestre el aviso.

## 4. Aplicación por módulo

### 4.1 Propuestas + Clientes: aceptar una propuesta

`PropuestaService.MarcarComoAceptada` llama a `ClienteService.ConvertirDesdeProspecto`, que también es transaccional. Como la transacción es reentrante, **las dos comparten la misma**:

```mermaid
sequenceDiagram
    participant C as PropuestasController
    participant P as PropuestaService
    participant U as TransaccionService
    participant CL as ClienteService
    participant DB as Base de datos
    C->>P: MarcarComoAceptada(id)
    P->>U: EjecutarAsync(...)
    U->>DB: BEGIN
    P->>CL: ConvertirDesdeProspecto(...)
    CL->>U: EjecutarAsync(...) [anidada]
    Note over U: Ya hay transacción, ejecuta directo
    CL->>DB: crea Cliente, prospecto pasa a "convertido"
    CL->>DB: bitácora (Clientes, Prospectos)
    P->>DB: propuesta pasa a "aceptada" + crea Expediente
    P->>DB: bitácora (Propuestas, Expedientes)
    U->>DB: COMMIT
    Note over DB: Si algo falla en cualquier punto:<br/>ROLLBACK de todo, incluido el cliente
```

### 4.2 Facturas: abonos con comprobante (archivo en disco)

El disco no es transaccional. Se resuelve registrando **antes de escribir** qué hacer si hay que deshacer:

```mermaid
sequenceDiagram
    participant F as FacturaService.RegistrarAbono
    participant U as TransaccionService
    participant DB as Base de datos
    participant D as Disco
    U->>DB: BEGIN
    F->>DB: SaveChanges (inserta abono, obtiene su Id)
    F->>U: AlRevertir(borrar carpeta del abono)
    F->>D: escribe el comprobante
    F->>DB: actualiza la factura, Registrar(bitácora)
    alt todo bien
        U->>DB: COMMIT (el archivo queda)
    else algo falla
        U->>DB: ROLLBACK
        U->>D: ejecuta AlRevertir, borra la carpeta
    end
```

Para **borrar** archivos ocurre al revés: no se borran hasta después del commit (`AlConfirmar`), para no perder un archivo si la transacción se revierte. Aplica a `EliminarTarea` y `EliminarArchivoEntregable`.

### 4.3 Usuarios y Roles (Identity)

No se crearon servicios nuevos: las acciones de los controllers se envuelven con `EjecutarIdentityAsync`. Cada paso de Identity devuelve un `IdentityResult`; `TransaccionFallidaException.Exigir(...)` lo convierte en excepción si falló, lo que provoca el rollback de los pasos anteriores.

```mermaid
flowchart TD
    A["UsuariosController.Crear"] --> B["EjecutarIdentityAsync"]
    B --> C["CreateAsync(usuario)"]
    C --> D{"¿Succeeded?"}
    D -->|No| X["TransaccionFallidaException"]
    D -->|Sí| E["AddToRoleAsync(rol)"]
    E --> F{"¿Succeeded?"}
    F -->|No| X
    F -->|Sí| G["Registrar(crear, Usuarios)"]
    G --> H["COMMIT"]
    X --> R["ROLLBACK: el usuario no queda creado.<br/>Ya no hace falta el DeleteAsync manual"]
    R --> V["El controller recibe el IdentityResult<br/>y muestra los errores en el formulario"]
```

Lo que se audita ahora:

| Acción | Tipo | Módulo | Valores |
|---|---|---|---|
| Crear usuario | `crear` | Usuarios | nombre, correo y rol |
| Editar usuario | `editar` | Usuarios | solo los campos que cambiaron (nombre, correo, especialidad, rol) |
| Activar / desactivar usuario | `cambiar_estado` | Usuarios | `activo` ↔ `inactivo` |
| Crear rol | `crear` | Roles | nombre y descripción |
| Editar rol | `editar` | Roles | nombre y descripción, solo si cambiaron |
| Activar / desactivar rol | `cambiar_estado` | Roles | `activo` ↔ `inactivo` |
| Guardar permisos de un rol | `editar` | Roles | lista completa de permisos **antes** y **después** |

Guardar permisos de un rol es lo que pide RF-001 ("historial completo de asignaciones y revocaciones"), y ahora es atómico: o se aplican todos los cambios de permisos o ninguno.

### 4.4 Notificaciones

Se envían **después** del commit y sin revertir la operación si fallan. Aprobar o devolver un entregable ya quedó hecho aunque no llegue la notificación (el error se registra en el log).

### 4.5 Cobertura

| Módulo | Operaciones dentro de la transacción |
|---|---|
| Propuestas | Crear, Editar, Aceptar |
| Clientes | Convertir desde prospecto |
| Facturas | Registrar abono, Anular factura, Anular abono |
| Expedientes | Cerrar (genera factura), Aprobar/Devolver entregable, Adjuntar archivo, Eliminar archivo, Eliminar tarea |
| Pro bono | Crear solicitud, Resolver (aprobar/rechazar) |
| Usuarios | Crear, Editar, Activar, Desactivar |
| Roles | Crear, Editar, Activar, Desactivar, Guardar permisos |

**Fuera del alcance de este MT** (siguen guardando en dos pasos, sin transacción): el resto de operaciones simples de Expedientes (agregar/editar/iniciar tarea, horas, reasignar), Propuestas (enviar, rechazar), Clientes (editar, activar, reasignar), Prospectos, Actividades de seguimiento, Servicios y Calidad. El mecanismo ya existe, aplicarlo es mecánico. Tampoco se registran aún los intentos de acceso no autorizado ni los inicios de sesión (RNF-001), porque requiere cambiar el modelo de la bitácora: decisión aparte.

## 5. Cómo se probó

Se inyectó un **fallo real** con un trigger temporal `AFTER INSERT` sobre `BitacoraAuditoria` que lanza un error para una acción concreta. Se ejecutó la operación desde la aplicación y se comparó la base antes y después. El trigger se eliminó al terminar.

| Operación con fallo forzado en su último paso | Resultado |
|---|---|
| Aceptar propuesta de un prospecto | Sin cliente nuevo, prospecto sigue "activo", propuesta sigue "enviada", sin expediente, sin filas de bitácora |
| Crear usuario | No queda usuario |
| Guardar permisos de un rol (2 altas + 1 baja) | Permisos sin cambios |
| Abono con comprobante | Sin abono, factura sin cambios, **sin carpeta en disco** |
| Aprobar entregable con archivo | Sin revisión, tarea sigue "lista para revisión", **archivo de la revisión borrado** |

Y con el fallo desactivado se comprobó que el camino normal funciona: aceptar propuesta (cliente + expediente + 4 registros), usuarios (crear/editar/desactivar/activar), roles (crear/editar/permisos/desactivar/activar), abono con comprobante, anular abono, adjuntar y eliminar archivos, eliminar tarea (archivos borrados tras el commit), aprobar entregable, cerrar expediente con su factura y notificaciones enviadas.

Las tres auditorías perdidas ahora sí quedan registradas (adjuntar archivo, eliminar archivo, eliminar tarea).

## 6. Cómo usarlo al agregar código nuevo

- Una operación que escribe en la base y audita: envolverla con `EjecutarAsync`.
- Dentro de la transacción puede haber varios `SaveChangesAsync()` y `Registrar(...)`; el guardado final cubre lo pendiente.
- Escribir un archivo: llamar a `AlRevertir(...)` **antes** de escribirlo, para que se limpie si algo falla.
- Borrar un archivo: usar `AlConfirmar(...)`, nunca borrarlo antes del commit.
- Pasos de Identity encadenados: `EjecutarIdentityAsync` + `TransaccionFallidaException.Exigir(...)`.
- Enviar una notificación: fuera de la transacción, después del commit.

## 7. Límites conocidos

- **Aislamiento:** se usa el predeterminado de SQL Server (READ COMMITTED). Evita datos a medias, pero no cierra por sí solo las carreras entre dos usuarios simultáneos (por ejemplo, dos administradores desactivándose a la vez, el caso "último administrador activo").
- **Archivos huérfanos por caída del proceso:** si la aplicación muere entre escribir un archivo y hacer el rollback, el archivo queda en disco. No es una inconsistencia de la base de datos y es raro.
- **Commit ambiguo:** si la conexión se cae justo durante el `COMMIT`, SQL Server pudo haberlo confirmado aunque el cliente vea un error transitorio; el reintento repetiría la operación. Es una ventana de milisegundos y EF no la cubre sin una verificación propia (`verifySucceeded`), que no se implementó.
- **Errores transitorios que se transforman antes de llegar a la estrategia:** varios servicios capturan `DbUpdateException` alrededor de su primer `SaveChanges` y lo relanzan como `ReglaNegocioException` con el mensaje de SQL. Si el error transitorio ocurre justo ahí, no se reintenta. El caso típico (la base pausada, que falla al abrir la conexión) no pasa por ahí.
- **Tiempo de conexión:** la cadena de conexión de Azure debería llevar `Connect Timeout=30` (predeterminado 15 s) para que cada intento aguante la reanudación; se configura en Azure, no en `appsettings.json`.
- **Carpetas vacías:** al borrar los archivos de una tarea queda su carpeta vacía (ya ocurría antes).
