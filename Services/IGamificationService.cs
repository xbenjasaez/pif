using BibliotecaVirtualWeb.Models;

namespace BibliotecaVirtualWeb.Services
{
    public interface IGamificationService
    {
        Task<List<Logro>> VerificarLogrosAsync(int usuarioId);
        Task<List<UsuarioLogro>> ObtenerLogrosUsuarioAsync(int usuarioId);
    }
}

