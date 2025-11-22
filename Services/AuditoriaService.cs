using System.Security.Claims;
using BibliotecaVirtualWeb.Data;
using BibliotecaVirtualWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaVirtualWeb.Services
{
    public class AuditoriaService : IAuditoriaService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditoriaService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task RegistrarAsync(string accion, string? detalle = null, ClaimsPrincipal? usuario = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var user = usuario ?? httpContext?.User;

            var registro = new Auditoria
            {
                Accion = accion,
                Detalle = detalle,
                Fecha = DateTime.UtcNow,
                UsuarioId = user?.FindFirstValue(ClaimTypes.NameIdentifier),
                UsuarioEmail = user?.Identity?.Name,
                IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString()
            };

            _context.Auditorias.Add(registro);
            await _context.SaveChangesAsync();
        }
    }
}

