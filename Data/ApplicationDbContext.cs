using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using BibliotecaVirtualWeb.Models;

namespace BibliotecaVirtualWeb.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Libro> Libros { get; set; } = null!;
        public DbSet<Ejemplar> Ejemplares { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Proveedor> Proveedores { get; set; } = null!;
        public DbSet<Prestamo> Prestamos { get; set; } = null!;
        public DbSet<Auditoria> Auditorias { get; set; } = null!;
        public DbSet<SistemaAlerta> Alertas { get; set; } = null!;
        public DbSet<BackupRegistro> BackupRegistros { get; set; } = null!;
        public DbSet<Logro> Logros { get; set; } = null!;
        public DbSet<UsuarioLogro> UsuarioLogros { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de Libro
            modelBuilder.Entity<Libro>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Titulo).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Autor).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ISBN).HasMaxLength(20);
                entity.Property(e => e.Categoria).HasMaxLength(50);
                entity.Property(e => e.Editorial).HasMaxLength(100);
                entity.Property(e => e.Descripcion).HasMaxLength(1000);
                entity.Property(e => e.Ubicacion).HasMaxLength(100);
                entity.Property(e => e.Estado).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PrestadoA).HasMaxLength(100);
                entity.Property(e => e.Notas).HasMaxLength(500);
                entity.Property(e => e.CodigoBarras).HasMaxLength(20);
                
                // Mapear Año a Ano para MySQL (para evitar problemas con caracteres especiales)
                // Nota: Este mapeo se aplica a ambos proveedores, pero MySQL usará "Ano" y SQLite seguirá usando "Año"
                entity.Property(e => e.Año).HasColumnName("Ano");

                entity.HasOne(e => e.Proveedor)
                      .WithMany()
                      .HasForeignKey(e => e.ProveedorId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configuración de Usuario
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Apellido).IsRequired().HasMaxLength(50);
                entity.Property(e => e.RUT).IsRequired().HasMaxLength(12);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Telefono).HasMaxLength(20);
                entity.Property(e => e.Estado).IsRequired().HasMaxLength(20);
                entity.Property(e => e.TipoUsuario).IsRequired().HasMaxLength(20).HasDefaultValue("Alumno");
                entity.Property(e => e.Notas).HasMaxLength(500);

                entity.HasIndex(e => e.RUT).IsUnique();
            });

            // Configuración de Proveedor
            modelBuilder.Entity<Proveedor>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Contacto).HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Telefono).HasMaxLength(20);
                entity.Property(e => e.Tipo).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Notas).HasMaxLength(500);
            });

            // Configuración de Ejemplar
            modelBuilder.Entity<Ejemplar>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CodigoBarras).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Estado).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PrestadoA).HasMaxLength(100);
                entity.Property(e => e.Notas).HasMaxLength(500);

                entity.HasIndex(e => e.CodigoBarras).IsUnique();

                entity.HasOne(e => e.Libro)
                      .WithMany()
                      .HasForeignKey(e => e.LibroId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de Préstamo
            modelBuilder.Entity<Prestamo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Estado).IsRequired().HasMaxLength(20);

                entity.HasOne(e => e.Ejemplar)
                      .WithMany()
                      .HasForeignKey(e => e.EjemplarId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Libro)
                      .WithMany()
                      .HasForeignKey(e => e.LibroId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Usuario)
                      .WithMany()
                      .HasForeignKey(e => e.UsuarioId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Auditoria>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Accion).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Detalle).HasMaxLength(500);
                entity.Property(e => e.UsuarioId).HasMaxLength(450);
                entity.Property(e => e.UsuarioEmail).HasMaxLength(150);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.Fecha).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.Fecha);
                entity.HasIndex(e => e.UsuarioId);
            });

            modelBuilder.Entity<SistemaAlerta>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Titulo).IsRequired().HasMaxLength(120);
                entity.Property(e => e.Mensaje).HasMaxLength(500);
                entity.Property(e => e.Tipo).IsRequired().HasMaxLength(20);
                entity.Property(e => e.DetalleTecnico).HasMaxLength(1000);
                entity.Property(e => e.Fecha).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.Resuelto);
                entity.HasIndex(e => e.Fecha);
            });

            // Configuración de Gamificación
            modelBuilder.Entity<UsuarioLogro>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Usuario)
                      .WithMany(u => u.UsuarioLogros)
                      .HasForeignKey(e => e.UsuarioId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Logro)
                      .WithMany()
                      .HasForeignKey(e => e.LogroId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Datos de ejemplo
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Logros
            modelBuilder.Entity<Logro>().HasData(
                new Logro { Id = 1, Nombre = "Primeros Pasos", Descripcion = "Realizar tu primer préstamo", Icono = "fa-book-reader", Color = "primary", CodigoInterno = "PRIMER_PRESTAMO", Puntos = 10 },
                new Logro { Id = 2, Nombre = "Lector Constante", Descripcion = "Completar 5 préstamos", Icono = "fa-book-open", Color = "info", CodigoInterno = "5_PRESTAMOS", Puntos = 50 },
                new Logro { Id = 3, Nombre = "Devorador de Libros", Descripcion = "Completar 10 préstamos", Icono = "fa-crown", Color = "warning", CodigoInterno = "10_PRESTAMOS", Puntos = 100 },
                new Logro { Id = 4, Nombre = "Puntualidad Perfecta", Descripcion = "Devolver 3 libros a tiempo consecutivos", Icono = "fa-clock", Color = "success", CodigoInterno = "PUNTUALIDAD_3", Puntos = 30 }
            );

            // Proveedores de ejemplo
            modelBuilder.Entity<Proveedor>().HasData(
                new Proveedor
                {
                    Id = 1,
                    Nombre = "Fundación Educativa",
                    Contacto = "Juan Pérez",
                    Email = "contacto@fundacion.edu",
                    Telefono = "+56 2 2345 6789",
                    Tipo = "Donacion",
                    FechaRegistro = DateTime.Now.AddDays(-30)
                },
                new Proveedor
                {
                    Id = 2,
                    Nombre = "Editorial Nacional",
                    Contacto = "María Silva",
                    Email = "ventas@editorial.com",
                    Telefono = "+56 2 3456 7890",
                    Tipo = "Compra",
                    FechaRegistro = DateTime.Now.AddDays(-20)
                }
            );

            // Usuarios de ejemplo
            modelBuilder.Entity<Usuario>().HasData(
                new Usuario
                {
                    Id = 1,
                    Nombre = "María",
                    Apellido = "González",
                    RUT = "12345678-9",
                    Email = "maria.gonzalez@email.com",
                    Telefono = "+56 9 1234 5678",
                    Estado = "Activo",
                    FechaRegistro = DateTime.Now.AddDays(-15)
                },
                new Usuario
                {
                    Id = 2,
                    Nombre = "Carlos",
                    Apellido = "López",
                    RUT = "87654321-0",
                    Email = "carlos.lopez@email.com",
                    Telefono = "+56 9 8765 4321",
                    Estado = "Activo",
                    FechaRegistro = DateTime.Now.AddDays(-10)
                }
            );

            // Libros de ejemplo
            modelBuilder.Entity<Libro>().HasData(
                new Libro
                {
                    Id = 1,
                    Titulo = "Cien años de soledad",
                    Autor = "Gabriel García Márquez",
                    ISBN = "978-84-376-0494-7",
                    Categoria = "Ficción",
                    Año = 1967,
                    Editorial = "Editorial Sudamericana",
                    Descripcion = "Una obra maestra del realismo mágico que narra la historia de la familia Buendía a lo largo de siete generaciones.",
                    Ubicacion = "Estante A, Fila 1",
                    Estado = "Disponible",
                    FechaAgregado = DateTime.Now.AddDays(-25),
                    CodigoBarras = "9781234567890",
                    ProveedorId = 1
                },
                new Libro
                {
                    Id = 2,
                    Titulo = "El Principito",
                    Autor = "Antoine de Saint-Exupéry",
                    ISBN = "978-84-376-0495-4",
                    Categoria = "Ficción",
                    Año = 1943,
                    Editorial = "Reynal & Hitchcock",
                    Descripcion = "Una fábula poética sobre la amistad, el amor y la pérdida de la inocencia.",
                    Ubicacion = "Estante B, Fila 2",
                    Estado = "Prestado",
                    FechaAgregado = DateTime.Now.AddDays(-20),
                    CodigoBarras = "9781234567891",
                    PrestadoA = "María González",
                    FechaPrestamo = DateTime.Now.AddDays(-5),
                    ProveedorId = 1
                },
                new Libro
                {
                    Id = 3,
                    Titulo = "Sapiens: De animales a dioses",
                    Autor = "Yuval Noah Harari",
                    ISBN = "978-84-376-0496-1",
                    Categoria = "Historia",
                    Año = 2011,
                    Editorial = "Debate",
                    Descripcion = "Un relato fascinante de la evolución de la humanidad desde la aparición del Homo sapiens.",
                    Ubicacion = "Estante C, Fila 1",
                    Estado = "Disponible",
                    FechaAgregado = DateTime.Now.AddDays(-15),
                    CodigoBarras = "9781234567892",
                    ProveedorId = 2
                }
            );
        }
    }
}
