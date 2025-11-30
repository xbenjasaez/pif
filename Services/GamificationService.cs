using BibliotecaVirtualWeb.Data;
using BibliotecaVirtualWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaVirtualWeb.Services
{
    public class GamificationService : IGamificationService
    {
        private readonly ApplicationDbContext _context;

        public GamificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<UsuarioLogro>> ObtenerLogrosUsuarioAsync(int usuarioId)
        {
            return await _context.UsuarioLogros
                .Include(ul => ul.Logro)
                .Where(ul => ul.UsuarioId == usuarioId)
                .OrderByDescending(ul => ul.FechaObtencion)
                .ToListAsync();
        }

        public async Task<List<Logro>> VerificarLogrosAsync(int usuarioId)
        {
            var nuevosLogros = new List<Logro>();
            var usuario = await _context.Usuarios
                .Include(u => u.UsuarioLogros)
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null) return nuevosLogros;

            // Obtener historial completo de préstamos para cálculos
            // Incluimos devueltos y activos
            var historialPrestamos = await _context.Prestamos
                .Where(p => p.UsuarioId == usuarioId)
                .OrderBy(p => p.FechaPrestamo)
                .ToListAsync();

            var totalPrestamos = historialPrestamos.Count;
            var logrosExistentesIds = usuario.UsuarioLogros.Select(ul => ul.LogroId).ToList();

            // 1. PRIMER_PRESTAMO
            if (totalPrestamos >= 1)
            {
                await AsignarLogroSiNoExiste("PRIMER_PRESTAMO", usuario, logrosExistentesIds, nuevosLogros);
            }

            // 2. 5_PRESTAMOS
            if (totalPrestamos >= 5)
            {
                await AsignarLogroSiNoExiste("5_PRESTAMOS", usuario, logrosExistentesIds, nuevosLogros);
            }

            // 3. 10_PRESTAMOS
            if (totalPrestamos >= 10)
            {
                await AsignarLogroSiNoExiste("10_PRESTAMOS", usuario, logrosExistentesIds, nuevosLogros);
            }

            // 4. PUNTUALIDAD_3 (3 devoluciones consecutivas a tiempo)
            // Filtramos solo los que tienen fecha de devolución (ya devueltos)
            var devueltos = historialPrestamos
                .Where(p => p.FechaDevolucion.HasValue)
                .OrderByDescending(p => p.FechaDevolucion)
                .ToList();

            if (devueltos.Count >= 3)
            {
                bool rachaPuntual = true;
                for (int i = 0; i < 3; i++)
                {
                    var prestamo = devueltos[i];
                    // Si se devolvió después de la fecha de vencimiento (dando un margen de gracia de 0 días, o sea estricto)
                    if (prestamo.FechaDevolucion > prestamo.FechaVencimiento)
                    {
                        rachaPuntual = false;
                        break;
                    }
                }

                if (rachaPuntual)
                {
                    await AsignarLogroSiNoExiste("PUNTUALIDAD_3", usuario, logrosExistentesIds, nuevosLogros);
                }
            }

            if (nuevosLogros.Any())
            {
                await _context.SaveChangesAsync();
            }

            return nuevosLogros;
        }

        private async Task AsignarLogroSiNoExiste(string codigoInterno, Usuario usuario, List<int> logrosExistentesIds, List<Logro> nuevosLogros)
        {
            var logro = await _context.Logros.FirstOrDefaultAsync(l => l.CodigoInterno == codigoInterno);
            if (logro != null && !logrosExistentesIds.Contains(logro.Id))
            {
                var nuevoUsuarioLogro = new UsuarioLogro
                {
                    UsuarioId = usuario.Id,
                    LogroId = logro.Id,
                    FechaObtencion = DateTime.Now
                };
                _context.UsuarioLogros.Add(nuevoUsuarioLogro);
                nuevosLogros.Add(logro);
                
                // Actualizar lista en memoria para evitar duplicados en la misma ejecución
                logrosExistentesIds.Add(logro.Id); 
            }
        }
    }
}

