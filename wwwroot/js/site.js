// JavaScript personalizado para Biblioteca Virtual

$(document).ready(function() {
    const THEME_KEY = 'bv-theme';
    const themeQuery = window.matchMedia ? window.matchMedia('(prefers-color-scheme: dark)') : null;

    function updateThemeIcons(theme) {
        document.querySelectorAll('[data-theme-toggle]').forEach(btn => {
            const icon = btn.querySelector('i');
            if (!icon) return;
            icon.classList.remove('fa-moon', 'fa-sun');
            icon.classList.add(theme === 'dark' ? 'fa-sun' : 'fa-moon');
        });
    }

    function setTheme(theme, persist = true) {
        document.documentElement.setAttribute('data-theme', theme);
        if (persist) {
            localStorage.setItem(THEME_KEY, theme);
        }
        updateThemeIcons(theme);
    }

    const storedTheme = localStorage.getItem(THEME_KEY);
    const prefersDark = themeQuery && themeQuery.matches;
    const initialTheme = document.documentElement.getAttribute('data-theme') || storedTheme || (prefersDark ? 'dark' : 'light');
    setTheme(initialTheme, false);

    document.querySelectorAll('[data-theme-toggle]').forEach(btn => {
        btn.addEventListener('click', () => {
            const current = document.documentElement.getAttribute('data-theme') || 'light';
            const nextTheme = current === 'dark' ? 'light' : 'dark';
            setTheme(nextTheme);
        });
    });

    if (themeQuery) {
        const syncSystemTheme = (event) => {
            if (!localStorage.getItem(THEME_KEY)) {
                setTheme(event.matches ? 'dark' : 'light', false);
            }
        };

        if (themeQuery.addEventListener) {
            themeQuery.addEventListener('change', syncSystemTheme);
        } else if (themeQuery.addListener) {
            themeQuery.addListener(syncSystemTheme);
        }
    }

    // Auto-hide alerts después de 15 segundos
    setTimeout(function() {
        $('.alert').fadeOut('slow');
    }, 15000);

    // Confirmación para eliminar elementos
    $('a[href*="Delete"]').click(function(e) {
        if (!confirm('¿Está seguro de que desea eliminar este elemento? Esta acción no se puede deshacer.')) {
            e.preventDefault();
        }
    });

    // Validación de RUT en tiempo real
    $('input[name="RUT"]').on('input', function() {
        var rut = $(this).val();
        var input = $(this);
        
        if (rut.length > 0) {
            // Aquí podrías agregar validación de RUT en tiempo real
            // Por ahora solo formateamos
            if (rut.length > 2 && !rut.includes('-')) {
                var numero = rut.slice(0, -1);
                var dv = rut.slice(-1);
                if (numero.length > 0) {
                    var formateado = numero.replace(/\B(?=(\d{3})+(?!\d))/g, '.') + '-' + dv;
                    input.val(formateado);
                }
            }
        }
    });

    // Tooltips
    $('[data-bs-toggle="tooltip"]').tooltip();

    // Popovers
    $('[data-bs-toggle="popover"]').popover();

    // Smooth scroll para enlaces internos
    $('a[href^="#"]').click(function(e) {
        e.preventDefault();
        var target = $(this.getAttribute('href'));
        if (target.length) {
            $('html, body').animate({
                scrollTop: target.offset().top - 100
            }, 1000);
        }
    });

    // Animación de entrada para cards
    $('.card').each(function(index) {
        $(this).css('animation-delay', (index * 0.1) + 's');
        $(this).addClass('fade-in');
    });

    // Búsqueda en tiempo real (si está implementada)
    $('#searchInput').on('input', function() {
        var searchTerm = $(this).val().toLowerCase();
        $('.searchable-item').each(function() {
            var text = $(this).text().toLowerCase();
            if (text.includes(searchTerm)) {
                $(this).show();
            } else {
                $(this).hide();
            }
        });
    });

    // Formateo automático de números
    $('input[type="number"]').on('input', function() {
        var value = $(this).val();
        if (value && value < 0) {
            $(this).val(0);
        }
    });

    // Validación de formularios
    $('form').on('submit', function() {
        var form = $(this);
        if (form[0].checkValidity() === false) {
            event.preventDefault();
            event.stopPropagation();
        }
        form.addClass('was-validated');
    });

    // Auto-resize textareas
    $('textarea').on('input', function() {
        this.style.height = 'auto';
        this.style.height = (this.scrollHeight) + 'px';
    });

    // Loading state para botones de envío
    $('form').on('submit', function() {
        var submitBtn = $(this).find('button[type="submit"]');
        if (submitBtn.length) {
            submitBtn.prop('disabled', true);
            submitBtn.html('<span class="spinner-border spinner-border-sm me-2" role="status"></span>Procesando...');
        }
    });

    // Confirmación para acciones importantes
    $('.btn-danger').click(function(e) {
        if (!confirm('¿Está seguro de que desea realizar esta acción?')) {
            e.preventDefault();
        }
    });

    // Toggle de vista (grid/list)
    $('#gridView').click(function() {
        $('#listViewContent').hide();
        $('#gridViewContent').show();
        $('#listView').removeClass('active');
        $(this).addClass('active');
        localStorage.setItem('viewMode', 'grid');
    });
    
    $('#listView').click(function() {
        $('#gridViewContent').hide();
        $('#listViewContent').show();
        $('#gridView').removeClass('active');
        $(this).addClass('active');
        localStorage.setItem('viewMode', 'list');
    });

    // Restaurar vista guardada
    var savedView = localStorage.getItem('viewMode');
    if (savedView === 'grid') {
        $('#gridView').click();
    }

    // Filtros dinámicos
    $('.filter-select').change(function() {
        $(this).closest('form').submit();
    });

    // Limpiar filtros
    $('#clearFilters').click(function() {
        $('input[type="text"], select').val('');
        $(this).closest('form').submit();
    });

    // Notificaciones toast (si se implementan)
    function showToast(message, type = 'info') {
        var toastHtml = `
            <div class="toast align-items-center text-white bg-${type} border-0" role="alert">
                <div class="d-flex">
                    <div class="toast-body">${message}</div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
                </div>
            </div>
        `;
        
        if (!$('#toastContainer').length) {
            $('body').append('<div id="toastContainer" class="toast-container position-fixed top-0 end-0 p-3"></div>');
        }
        
        $('#toastContainer').append(toastHtml);
        $('.toast').last().toast('show');
    }

    // Función global para mostrar notificaciones
    window.showNotification = showToast;

    // Atajos globales para préstamos y devoluciones rápidas
    $(document).on('keydown', function(event) {
        const tagName = (event.target.tagName || '').toLowerCase();
        const isEditable = event.target.isContentEditable;

        if (tagName === 'input' || tagName === 'textarea' || isEditable) {
            return;
        }

        // Ctrl + Shift + D: Devolución Rápida
        if (event.ctrlKey && event.shiftKey && (event.code === 'KeyD' || event.key.toLowerCase() === 'd')) {
            event.preventDefault();
            window.location.href = '/Prestamos/DevolucionRapida';
        }

        // Ctrl + Shift + P: Préstamo Rápido
        if (event.ctrlKey && event.shiftKey && (event.code === 'KeyP' || event.key.toLowerCase() === 'p')) {
            event.preventDefault();
            window.location.href = '/Prestamos/PrestamoRapido';
        }
    });
});

// Funciones utilitarias
function formatRUT(rut) {
    if (!rut) return '';
    
    // Limpiar RUT
    rut = rut.replace(/[^0-9kK]/g, '');
    
    if (rut.length < 2) return rut;
    
    var numero = rut.slice(0, -1);
    var dv = rut.slice(-1).toUpperCase();
    
    // Formatear número con puntos
    var formateado = numero.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
    
    return formateado + '-' + dv;
}

function validateRUT(rut) {
    if (!rut) return false;
    
    // Limpiar RUT
    rut = rut.replace(/[^0-9kK]/g, '');
    
    if (rut.length < 2) return false;
    
    var numero = rut.slice(0, -1);
    var dv = rut.slice(-1).toUpperCase();
    
    // Validar que el número sea numérico
    if (!/^\d+$/.test(numero)) return false;
    
    // Calcular dígito verificador
    var suma = 0;
    var multiplicador = 2;
    
    for (var i = numero.length - 1; i >= 0; i--) {
        suma += parseInt(numero[i]) * multiplicador;
        multiplicador = multiplicador === 7 ? 2 : multiplicador + 1;
    }
    
    var resto = suma % 11;
    var dvCalculado = 11 - resto;
    
    if (dvCalculado === 11) dvCalculado = '0';
    else if (dvCalculado === 10) dvCalculado = 'K';
    else dvCalculado = dvCalculado.toString();
    
    return dv === dvCalculado;
}

// Función para exportar datos (si se implementa)
function exportToCSV(data, filename) {
    var csv = 'data:text/csv;charset=utf-8,';
    csv += data.map(row => row.join(',')).join('\n');
    
    var encodedUri = encodeURI(csv);
    var link = document.createElement('a');
    link.setAttribute('href', encodedUri);
    link.setAttribute('download', filename);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

// Función para imprimir
function printPage() {
    window.print();
}

// Función para refrescar datos
function refreshData() {
    location.reload();
}
