namespace BibliotecaVirtualWeb.Models
{
    public class EjemplaresIndexViewModel
    {
        public List<Ejemplar> Ejemplares { get; set; } = new();
        public EjemplaresResumenViewModel Resumen { get; set; } = new();

        public int? LibroId { get; set; }
        public string LibroTitulo { get; set; } = "Todos los libros";

        public string? Busqueda { get; set; }
        public string? EstadoSeleccionado { get; set; }
        public string OrdenSeleccionado { get; set; } = "recientes";

        public IEnumerable<string> EstadosDisponibles { get; set; } = Enumerable.Empty<string>();
    }

    public class EjemplaresResumenViewModel
    {
        public int Total { get; set; }
        public int Disponibles { get; set; }
        public int Prestados { get; set; }
        public int Otros { get; set; }
    }
}

