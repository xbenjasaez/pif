using System.Threading.Tasks;

namespace BibliotecaVirtualWeb.Services
{
    public interface IAlertaSistemaService
    {
        Task RegistrarErrorAsync(string titulo, string mensaje, string? detalleTecnico = null);
        Task<IEnumerable<Models.SistemaAlerta>> ObtenerAlertasActivasAsync(int max = 5);
        Task ResolverAsync(int alertaId);
    }
}

