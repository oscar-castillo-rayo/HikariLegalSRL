using HikariLegalSRL.Controllers;
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
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Direccion_TipoUbicacion",
                    "[TipoUbicacion] IN ('nacional', 'extranjero')"
                ));

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
