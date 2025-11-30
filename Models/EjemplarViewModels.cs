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

        // Paginación
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
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

    public class EjemplarEstadisticasViewModel
    {
        public int TotalPrestamos { get; set; }
        public int PrestamosActivos { get; set; }
        public int DevolucionesATiempo { get; set; }
        public int DevolucionesTardias { get; set; }
        public int UsuariosUnicos { get; set; }
        public int PromedioDiasPrestamo { get; set; }
        public DateTime? UltimoPrestamo { get; set; }
        public DateTime? UltimaDevolucion { get; set; }
        
        public double PorcentajeATiempo => TotalPrestamos > 0 && (DevolucionesATiempo + DevolucionesTardias) > 0
            ? Math.Round((double)DevolucionesATiempo / (DevolucionesATiempo + DevolucionesTardias) * 100, 1)
            : 100;
    }
}

