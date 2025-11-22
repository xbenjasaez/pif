using BibliotecaVirtualWeb.Data;
using BibliotecaVirtualWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaVirtualWeb.Services
{
    public class AlertaSistemaService : IAlertaSistemaService
    {
        private readonly ApplicationDbContext _context;

        public AlertaSistemaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task RegistrarErrorAsync(string titulo, string mensaje, string? detalleTecnico = null)
        {
            var alerta = new SistemaAlerta
            {
                Titulo = titulo,
                Mensaje = mensaje,
                DetalleTecnico = detalleTecnico,
                Tipo = "Error",
                Fecha = DateTime.UtcNow
            };

            _context.Alertas.Add(alerta);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<SistemaAlerta>> ObtenerAlertasActivasAsync(int max = 5)
        {
            return await _context.Alertas
                .Where(a => !a.Resuelto)
                .OrderByDescending(a => a.Fecha)
                .Take(max)
                .ToListAsync();
        }

        public async Task ResolverAsync(int alertaId)
        {
            var alerta = await _context.Alertas.FindAsync(alertaId);
            if (alerta == null) return;

            alerta.Resuelto = true;
            alerta.FechaResuelto = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}

