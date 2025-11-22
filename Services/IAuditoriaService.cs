using System.Security.Claims;
using System.Threading.Tasks;

namespace BibliotecaVirtualWeb.Services
{
    public interface IAuditoriaService
    {
        Task RegistrarAsync(string accion, string? detalle = null, ClaimsPrincipal? usuario = null);
    }
}

