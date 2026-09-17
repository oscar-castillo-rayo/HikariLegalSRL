using HikariLegalSRL.Controllers.BitacoraAuditoria;
using HikariLegalSRL.Models;
using HikariLegalSRL.Models.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Pais> Paises { get; set; } = null!;
        public DbSet<Provincia> Provincias { get; set; } = null!;
        public DbSet<Canton> Cantones { get; set; } = null!;
        public DbSet<Distrito> Distritos { get; set; } = null!;
        public DbSet<Direccion> Direcciones { get; set; } = null!;
        public DbSet<Prospecto> Prospectos { get; set; } = null!;
        public DbSet<Cliente> Clientes { get; set; } = null!;
        public DbSet<ActividadSeguimiento> ActividadesSeguimiento { get; set; } = null!;
        public DbSet<CatalogoServicio> CatalogoServicios { get; set; } = null!;
        public DbSet<Propuesta> Propuestas { get; set; } = null!;
        public DbSet<PropuestaServicio> PropuestaServicios { get; set; } = null!;
        public DbSet<Expediente> Expedientes { get; set; } = null!;
        public DbSet<Tarea> Tareas { get; set; } = null!;
        public DbSet<Entregable> Entregables { get; set; } = null!;
        public DbSet<RegistroHoras> RegistrosHoras { get; set; } = null!;
        public DbSet<BitacoraAuditoria> BitacoraAuditoria { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Paises
            modelBuilder.Entity<Pais>()
                .HasIndex(p => p.Nombre)
                .IsUnique();

            // Provincias
            modelBuilder.Entity<Provincia>()
                .HasIndex(p => new { p.PaisId, p.Nombre })
                .IsUnique();

            modelBuilder.Entity<Provincia>()
                .HasOne(p => p.Pais)
                .WithMany(pa => pa.Provincias)
                .HasForeignKey(p => p.PaisId)
                .OnDelete(DeleteBehavior.NoAction);

            // Cantones
            modelBuilder.Entity<Canton>()
                .HasIndex(c => new { c.ProvinciaId, c.Nombre })
                .IsUnique();

            modelBuilder.Entity<Canton>()
                .HasOne(c => c.Provincia)
                .WithMany(p => p.Cantones)
                .HasForeignKey(c => c.ProvinciaId)
                .OnDelete(DeleteBehavior.NoAction);

            // Distritos
            modelBuilder.Entity<Distrito>()
                .HasIndex(d => new { d.CantonId, d.Nombre })
                .IsUnique();

            modelBuilder.Entity<Distrito>()
                .HasOne(d => d.Canton)
                .WithMany(c => c.Distritos)
                .HasForeignKey(d => d.CantonId)
                .OnDelete(DeleteBehavior.NoAction);

            // Seed para insertar Costa Rica como país base
            modelBuilder.Entity<Pais>().HasData(new Pais { PaisId = 1, Nombre = "Costa Rica", EsPaisBase = true });

            // Direccion
            modelBuilder.Entity<Direccion>()
                .Property(d => d.SenasExactas).HasMaxLength(500);

            modelBuilder.Entity<Direccion>()
                .Property(d => d.TipoUbicacion).HasMaxLength(15);

            modelBuilder.Entity<Direccion>()
                .ToTable(t =>
                {
                    t.HasCheckConstraint(
                        "CK_Direccion_TipoUbicacion",
                        "[TipoUbicacion] IN ('nacional', 'extranjero')"
                    );
                    t.HasTrigger("TR_Direcciones_TipoUbicacion");
                    t.HasTrigger("TR_Direcciones_Validar");
                });

            modelBuilder.Entity<Direccion>()
                .HasOne(d => d.Pais)
                .WithMany()
                .HasForeignKey(d => d.PaisId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Direccion>()
                .HasOne(d => d.Distrito)
                .WithMany()
                .HasForeignKey(d => d.DistritoId)
                .OnDelete(DeleteBehavior.NoAction);

            // Prospecto
            modelBuilder.Entity<Prospecto>()
                .Property(p => p.NombreEmpresaPersona).HasMaxLength(200);
            modelBuilder.Entity<Prospecto>()
                .Property(p => p.NombreContacto).HasMaxLength(150);
            modelBuilder.Entity<Prospecto>()
                .Property(p => p.CedulaJuridica).HasMaxLength(30);
            modelBuilder.Entity<Prospecto>()
                .Property(p => p.Telefono).HasMaxLength(30);
            modelBuilder.Entity<Prospecto>()
                .Property(p => p.Correo).HasMaxLength(150);
            modelBuilder.Entity<Prospecto>()
                .Property(p => p.Sector).HasMaxLength(100);
            modelBuilder.Entity<Prospecto>()
                .Property(p => p.Estado)
                .HasConversion(e => e.ToString().ToLower(),
                s => (EstadoProspecto)Enum.Parse(typeof(EstadoProspecto), s, ignoreCase: true))
                .HasMaxLength(15);

            modelBuilder.Entity<Prospecto>()
                .HasOne(p => p.Direccion)
                .WithMany()
                .HasForeignKey(p => p.DireccionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Prospecto>()
                .HasOne(p => p.UsuarioCreador)
                .WithMany()
                .HasForeignKey(p => p.UsuarioCreadorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Prospecto>()
                .HasIndex(p => p.Correo)
                .IsUnique();

            modelBuilder.Entity<Prospecto>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Prospecto_Calificacion",
                    "[Calificacion] IS NULL OR ([Calificacion] BETWEEN 1 AND 5)"
                    ));

            modelBuilder.Entity<Prospecto>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Prospecto_Estado",
                    "[Estado] IN ('activo', 'convertido', 'descartado')"
                    ));

            // Cliente
            modelBuilder.Entity<Cliente>()
                .Property(c => c.NombreEmpresaPersona).HasMaxLength(200);
            modelBuilder.Entity<Cliente>()
                .Property(c => c.NombreContacto).HasMaxLength(150);
            modelBuilder.Entity<Cliente>()
                .Property(c => c.CedulaJuridica).HasMaxLength(30);
            modelBuilder.Entity<Cliente>()
                .Property(c => c.Telefono).HasMaxLength(30);
            modelBuilder.Entity<Cliente>()
                .Property(c => c.Correo).HasMaxLength(150);
            modelBuilder.Entity<Cliente>()
                .Property(c => c.SectorEconomico).HasMaxLength(100);

            modelBuilder.Entity<Cliente>()
                .Property(c => c.ModalidadPago)
                .HasConversion(
                    m => m == ModalidadPago.ProBono ? "pro_bono" : m.ToString().ToLower(),
                    s => s == "pro_bono" ? ModalidadPago.ProBono : (ModalidadPago)Enum.Parse(typeof(ModalidadPago), s, true))
                .HasMaxLength(15);

            modelBuilder.Entity<Cliente>()
                .Property(c => c.Estado)
                .HasConversion(e => e.ToString().ToLower(),
                s => (EstadoCliente)Enum.Parse(typeof(EstadoCliente), s, ignoreCase: true))
                .HasMaxLength(10);

            modelBuilder.Entity<Cliente>()
                .HasOne(c => c.ProspectoOrigen)
                .WithMany()
                .HasForeignKey(c => c.ProspectoOrigenId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Cliente>()
                .HasOne(c => c.Direccion)
                .WithMany()
                .HasForeignKey(c => c.DireccionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Cliente>()
                .HasOne(c => c.Responsable)
                .WithMany()
                .HasForeignKey(c => c.ResponsableId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.Correo)
                .IsUnique();

            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.ProspectoOrigenId)
                .IsUnique()
                .HasFilter("[ProspectoOrigenId] IS NOT NULL");

            modelBuilder.Entity<Cliente>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Cliente_ModalidadPago",
                    "[ModalidadPago] IN ('contado', 'abono', 'pro_bono')"
                    ));

            modelBuilder.Entity<Cliente>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Cliente_Estado",
                    "[Estado] IN ('activo', 'inactivo')"
                    ));

            // ActividadesSeguimiento
            modelBuilder.Entity<ActividadSeguimiento>()
                .Property(a => a.TipoActividad)
                .HasConversion(e => e.ToString().ToLower(),
                s => (TipoActividad)Enum.Parse(typeof(TipoActividad), s, ignoreCase: true))
                .HasMaxLength(20);

            modelBuilder.Entity<ActividadSeguimiento>()
                .Property(a => a.Titulo).HasMaxLength(150);

            modelBuilder.Entity<ActividadSeguimiento>()
                .Property(a => a.Descripcion).HasMaxLength(1000);

            modelBuilder.Entity<ActividadSeguimiento>()
                .HasOne(a => a.Prospecto)
                .WithMany()
                .HasForeignKey(a => a.ProspectoId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ActividadSeguimiento>()
                .HasOne(a => a.UsuarioRegistro)
                .WithMany()
                .HasForeignKey(a => a.UsuarioRegistroId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ActividadSeguimiento>()
                .HasOne(a => a.Responsable)
                .WithMany()
                .HasForeignKey(a => a.ResponsableId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ActividadSeguimiento>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_ActividadSeguimiento_TipoActividad",
                    "[TipoActividad] IN ('llamada', 'reunion', 'correo', 'nota', 'propuesta', 'otro')"
                    ));

            // CatalogoServicio
            modelBuilder.Entity<CatalogoServicio>()
                .HasKey(s => s.ServicioId);
            modelBuilder.Entity<CatalogoServicio>()
                .Property(s => s.Nombre).HasMaxLength(150);
            modelBuilder.Entity<CatalogoServicio>()
                .Property(s => s.AreaCategoria).HasMaxLength(100);
            modelBuilder.Entity<CatalogoServicio>()
                .Property(s => s.Descripcion).HasMaxLength(1000);
            modelBuilder.Entity<CatalogoServicio>()
                .Property(s => s.PrecioBase).HasColumnType("decimal(14,2)");

            modelBuilder.Entity<CatalogoServicio>()
                .Property(s => s.TipoServicio)
                .HasConversion(e => e.ToString().ToLower(),
                s => (TipoServicio)Enum.Parse(typeof(TipoServicio), s, ignoreCase: true))
                .HasMaxLength(15);

            modelBuilder.Entity<CatalogoServicio>()
                .Property(s => s.Estado)
                .HasConversion(e => e.ToString().ToLower(),
                s => (EstadoServicio)Enum.Parse(typeof(EstadoServicio), s, ignoreCase: true))
                .HasMaxLength(10);

            modelBuilder.Entity<CatalogoServicio>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_CatalogoServicio_PrecioBase",
                    "[PrecioBase] >= 0"
                    ));

            modelBuilder.Entity<CatalogoServicio>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_CatalogoServicio_TipoServicio",
                    "[TipoServicio] IN ('ofrecido', 'solicitado')"
                    ));

            modelBuilder.Entity<CatalogoServicio>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_CatalogoServicio_Estado",
                    "[Estado] IN ('activo', 'inactivo')"
                    ));

            // Propuesta
            modelBuilder.Entity<Propuesta>()
                .HasKey(p => p.PropuestaId);

            modelBuilder.Entity<Propuesta>()
                .Property(p => p.DescripcionGeneral).HasMaxLength(2000);
            modelBuilder.Entity<Propuesta>()
                .Property(p => p.MontoTotal).HasColumnType("decimal(14,2)");

            modelBuilder.Entity<Propuesta>()
                .Property(p => p.Moneda)
                .HasConversion(e => e.ToString().ToLower(),
                s => (Moneda)Enum.Parse(typeof(Moneda), s, ignoreCase: true))
                .HasMaxLength(10);

            modelBuilder.Entity<Propuesta>()
                .Property(p => p.ModalidadPago)
                .HasConversion(
                    m => m == ModalidadPago.ProBono ? "pro_bono" : m.ToString().ToLower(),
                    s => s == "pro_bono" ? ModalidadPago.ProBono : (ModalidadPago)Enum.Parse(typeof(ModalidadPago), s, true))
                .HasMaxLength(15);

            modelBuilder.Entity<Propuesta>()
                .Property(p => p.Estado)
                .HasConversion(e => e.ToString().ToLower(),
                s => (EstadoPropuesta)Enum.Parse(typeof(EstadoPropuesta), s, ignoreCase: true))
                .HasMaxLength(15);

            modelBuilder.Entity<Propuesta>()
                .HasOne(p => p.Prospecto)
                .WithMany()
                .HasForeignKey(p => p.ProspectoId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Propuesta>()
                .HasOne(p => p.Cliente)
                .WithMany()
                .HasForeignKey(p => p.ClienteId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Propuesta>()
                .HasOne(p => p.ElaboradaPor)
                .WithMany()
                .HasForeignKey(p => p.ElaboradaPorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Propuesta>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Propuesta_Destinatario",
                    "([ProspectoId] IS NOT NULL AND [ClienteId] IS NULL) OR ([ProspectoId] IS NULL AND [ClienteId] IS NOT NULL)"
                    ));

            modelBuilder.Entity<Propuesta>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Propuesta_Moneda",
                    "[Moneda] IN ('colones', 'dolares')"
                    ));

            modelBuilder.Entity<Propuesta>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Propuesta_ModalidadPago",
                    "[ModalidadPago] IN ('contado', 'abono', 'pro_bono')"
                    ));

            modelBuilder.Entity<Propuesta>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Propuesta_Estado",
                    "[Estado] IN ('borrador', 'enviada', 'aceptada', 'rechazada')"
                    ));

            modelBuilder.Entity<Propuesta>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Propuesta_PlazoDias",
                    "[PlazoDias] > 0"
                    ));

            // PropuestaServicio
            modelBuilder.Entity<PropuestaServicio>()
                .HasKey(s => s.PropuestaServicioId);

            modelBuilder.Entity<PropuestaServicio>()
                .Property(s => s.DescripcionServicio).HasMaxLength(1000);
            modelBuilder.Entity<PropuestaServicio>()
                .Property(s => s.Precio).HasColumnType("decimal(14,2)");

            modelBuilder.Entity<PropuestaServicio>()
                .Property(s => s.TipoServicio)
                .HasConversion(e => e.ToString().ToLower(),
                s => (TipoServicio)Enum.Parse(typeof(TipoServicio), s, ignoreCase: true))
                .HasMaxLength(15);

            modelBuilder.Entity<PropuestaServicio>()
                .HasOne(s => s.Propuesta)
                .WithMany(p => p.Servicios)
                .HasForeignKey(s => s.PropuestaId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PropuestaServicio>()
                .HasOne(s => s.Servicio)
                .WithMany()
                .HasForeignKey(s => s.ServicioId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PropuestaServicio>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_PropuestaServicio_Precio",
                    "[Precio] >= 0"
                    ));

            modelBuilder.Entity<PropuestaServicio>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_PropuestaServicio_TipoServicio",
                    "[TipoServicio] IN ('ofrecido', 'solicitado')"
                    ));

            // Expediente
            modelBuilder.Entity<Expediente>()
                .HasKey(e => e.ExpedienteId);

            modelBuilder.Entity<Expediente>()
                .Property(e => e.PlazoComprometido).HasColumnType("date");

            modelBuilder.Entity<Expediente>()
                .Property(e => e.Estado)
                .HasConversion(e => e.ToString().ToLower(),
                s => (EstadoExpediente)Enum.Parse(typeof(EstadoExpediente), s, ignoreCase: true))
                .HasMaxLength(10);

            modelBuilder.Entity<Expediente>()
                .HasOne(e => e.Cliente)
                .WithMany()
                .HasForeignKey(e => e.ClienteId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Expediente>()
                .HasOne(e => e.Propuesta)
                .WithMany()
                .HasForeignKey(e => e.PropuestaId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Expediente>()
                .HasIndex(e => e.PropuestaId)
                .IsUnique();

            modelBuilder.Entity<Expediente>()
                .HasOne(e => e.Responsable)
                .WithMany()
                .HasForeignKey(e => e.ResponsableId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Expediente>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Expediente_Estado",
                    "[Estado] IN ('abierto', 'cerrado')"
                    ));

            // Tarea
            modelBuilder.Entity<Tarea>()
                .HasKey(t => t.TareaId);

            modelBuilder.Entity<Tarea>()
                .Property(t => t.Descripcion).HasMaxLength(1000);

            modelBuilder.Entity<Tarea>()
                .Property(t => t.HorasEstimadas).HasColumnType("decimal(6,2)");

            modelBuilder.Entity<Tarea>()
                .Property(t => t.FechaLimite).HasColumnType("date");

            modelBuilder.Entity<Tarea>()
                .Property(t => t.Prioridad)
                .HasConversion(e => e.ToString().ToLower(),
                s => (PrioridadTarea)Enum.Parse(typeof(PrioridadTarea), s, ignoreCase: true))
                .HasMaxLength(10);

            modelBuilder.Entity<Tarea>()
                .Property(t => t.Estado)
                .HasConversion(
                    e => e == EstadoTarea.Pendiente ? "pendiente"
                        : e == EstadoTarea.EnProceso ? "en_proceso"
                        : e == EstadoTarea.ListaRevision ? "lista_revision"
                        : e == EstadoTarea.Aprobada ? "aprobada"
                        : "devuelta",
                    s => s == "pendiente" ? EstadoTarea.Pendiente
                        : s == "en_proceso" ? EstadoTarea.EnProceso
                        : s == "lista_revision" ? EstadoTarea.ListaRevision
                        : s == "aprobada" ? EstadoTarea.Aprobada
                        : EstadoTarea.Devuelta)
                .HasMaxLength(20);

            modelBuilder.Entity<Tarea>()
                .HasOne(t => t.Expediente)
                .WithMany(e => e.Tareas)
                .HasForeignKey(t => t.ExpedienteId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Tarea>()
                .HasOne(t => t.ColaboradorResponsable)
                .WithMany()
                .HasForeignKey(t => t.ColaboradorResponsableId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Tarea>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Tarea_HorasEstimadas",
                    "[HorasEstimadas] >= 0"
                    ));

            modelBuilder.Entity<Tarea>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Tarea_Prioridad",
                    "[Prioridad] IN ('baja', 'media', 'alta')"
                    ));

            modelBuilder.Entity<Tarea>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Tarea_Estado",
                    "[Estado] IN ('pendiente', 'en_proceso', 'lista_revision', 'aprobada', 'devuelta')"
                    ));

            // Entregable
            modelBuilder.Entity<Entregable>()
                .HasKey(en => en.EntregableId);

            modelBuilder.Entity<Entregable>()
                .Property(en => en.ArchivoRuta).HasMaxLength(500);

            modelBuilder.Entity<Entregable>()
                .Property(en => en.HorasReales).HasColumnType("decimal(6,2)");

            modelBuilder.Entity<Entregable>()
                .Property(en => en.TipoEntregable)
                .HasConversion(e => e.ToString().ToLower(),
                s => (TipoEntregable)Enum.Parse(typeof(TipoEntregable), s, ignoreCase: true))
                .HasMaxLength(11);

            modelBuilder.Entity<Entregable>()
                .HasOne(en => en.Tarea)
                .WithMany(t => t.Entregables)
                .HasForeignKey(en => en.TareaId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Entregable>()
                .HasOne(en => en.CargadoPor)
                .WithMany()
                .HasForeignKey(en => en.CargadoPorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Entregable>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Entregable_HorasReales",
                    "[HorasReales] >= 0"
                    ));

            modelBuilder.Entity<Entregable>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Entregable_TipoEntregable",
                    "[TipoEntregable] IN ('preliminar', 'final')"
                    ));

            // RegistroHoras
            modelBuilder.Entity<RegistroHoras>()
                .HasKey(r => r.RegistroHorasId);

            modelBuilder.Entity<RegistroHoras>()
                .Property(r => r.Rol)
                .HasConversion(e => e.ToString().ToLower(),
                s => (RolHoras)Enum.Parse(typeof(RolHoras), s, ignoreCase: true))
                .HasMaxLength(12);

            modelBuilder.Entity<RegistroHoras>()
                .HasOne(r => r.Tarea)
                .WithMany(t => t.RegistrosHoras)
                .HasForeignKey(r => r.TareaId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RegistroHoras>()
                .HasOne(r => r.Usuario)
                .WithMany()
                .HasForeignKey(r => r.UsuarioId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RegistroHoras>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_RegistroHoras_Minutos",
                    "[Minutos] > 0"
                    ));

            modelBuilder.Entity<RegistroHoras>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_RegistroHoras_Rol",
                    "[Rol] IN ('colaborador', 'revisor')"
                    ));

            // Bitacora Auditoría
            modelBuilder.Entity<BitacoraAuditoria>()
                .Property(b => b.TipoAccion).HasMaxLength(20);
            modelBuilder.Entity<BitacoraAuditoria>()
                .Property(b => b.ModuloAfectado).HasMaxLength(100);
            modelBuilder.Entity<BitacoraAuditoria>()
                .Property(b => b.RegistroAfectadoId).HasMaxLength(50);
            modelBuilder.Entity<BitacoraAuditoria>()
                .HasIndex(b => b.FechaHora);
            modelBuilder.Entity<BitacoraAuditoria>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_BitacoraAuditoria_TipoAccion",
                    "[TipoAccion] IN ('crear', 'editar', 'eliminar', 'cambiar_estado', 'aprobar', 'rechazar')"
                    ));
            modelBuilder.Entity<BitacoraAuditoria>()
                .HasOne(b => b.Usuario)
                .WithMany()
                .HasForeignKey(b => b.UsuarioId)
                .OnDelete(DeleteBehavior.NoAction);



        }
    }
}
