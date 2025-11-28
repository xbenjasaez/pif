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

        /// <summary>
        /// Indica si se debe mostrar el mensaje de "realiza una búsqueda" en lugar de la tabla.
        /// </summary>
        public bool MostrarMensajeBusqueda { get; set; }
    }

    public class EjemplaresResumenViewModel
    {
        public int Total { get; set; }
        public int Disponibles { get; set; }
        public int Prestados { get; set; }
        public int Otros { get; set; }
    }

    public class EjemplaresImprimirViewModel
    {
        public List<Ejemplar> Ejemplares { get; set; } = new();
        public int? LibroId { get; set; }
        public string TituloDocumento { get; set; } = "Códigos de barras de ejemplares";
        public DateTime GeneradoEl { get; set; } = DateTime.Now;
        public bool AutoPrint { get; set; }
    }
}

