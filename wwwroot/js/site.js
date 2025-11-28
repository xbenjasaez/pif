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

    // Auto-hide alerts después de 15 segundos (excepto las permanentes)
    setTimeout(function() {
        $('.alert').not('.alert-permanent').fadeOut('slow');
    }, 15000);

    // Confirmación para eliminar elementos
    $('a[href*="Delete"]').click(function(e) {
        if (!confirm('¿Está seguro de que desea eliminar este elemento? Esta acción no se puede deshacer.')) {
            e.preventDefault();
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

    // Loading state para botones de envío
    $('form').on('submit', function() {
        var submitBtn = $(this).find('button[type="submit"]');
        if (submitBtn.length) {
            submitBtn.prop('disabled', true);
            submitBtn.html('<span class="spinner-border spinner-border-sm me-2" role="status"></span>Procesando...');
        }
    });

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

    initializeRutInputs();
});

const RUT_BODY_MAX_LENGTH = 9;
const RUT_TOTAL_MAX_LENGTH = RUT_BODY_MAX_LENGTH + 1;

function cleanRutValue(value) {
    if (!value) return '';
    return value.replace(/[^0-9kK]/g, '').toUpperCase();
}

// Funciones utilitarias
function formatRUT(rut) {
    let clean = cleanRutValue(rut);
    if (!clean) return '';

    if (clean.length > RUT_TOTAL_MAX_LENGTH) {
        clean = clean.slice(0, RUT_TOTAL_MAX_LENGTH);
    }

    if (clean.length <= 1) return clean;

    const cuerpo = clean.slice(0, -1);
    const dv = clean.slice(-1);
    const cuerpoFormateado = cuerpo.replace(/\B(?=(\d{3})+(?!\d))/g, '.');

    return `${cuerpoFormateado}-${dv}`;
}

function validateRUT(rut) {
    const clean = cleanRutValue(rut);
    if (clean.length < 2) return false;

    const numero = clean.slice(0, -1);
    const dv = clean.slice(-1).toUpperCase();

    if (!/^\d+$/.test(numero)) return false;

    let suma = 0;
    let multiplicador = 2;

    for (let i = numero.length - 1; i >= 0; i--) {
        suma += parseInt(numero[i], 10) * multiplicador;
        multiplicador = multiplicador === 7 ? 2 : multiplicador + 1;
    }

    const resto = suma % 11;
    let dvCalculado = 11 - resto;

    if (dvCalculado === 11) {
        dvCalculado = '0';
    } else if (dvCalculado === 10) {
        dvCalculado = 'K';
    } else {
        dvCalculado = dvCalculado.toString();
    }

    return dv === dvCalculado;
}

function updateRutValidationState(input) {
    const value = input.value.trim();
    if (!value) {
        input.classList.remove('is-valid', 'is-invalid');
        input.setCustomValidity('');
        return;
    }

    if (validateRUT(value)) {
        input.classList.add('is-valid');
        input.classList.remove('is-invalid');
        input.setCustomValidity('');
    } else {
        input.classList.remove('is-valid');
        input.classList.add('is-invalid');
        input.setCustomValidity('RUT inválido');
    }
}

function handleRutInput(event) {
    const input = event.target;
    const formatted = formatRUT(input.value);
    input.value = formatted;
    updateRutValidationState(input);
}

function initializeRutInputs() {
    const inputs = document.querySelectorAll('input.rut-input');
    inputs.forEach(input => {
        if (input.dataset.rutInitialized === 'true') {
            return;
        }
        input.dataset.rutInitialized = 'true';
        if (!input.hasAttribute('maxlength')) {
            input.setAttribute('maxlength', '12');
        }
        input.addEventListener('input', handleRutInput);
        input.addEventListener('blur', () => updateRutValidationState(input));
        input.addEventListener('focus', () => updateRutValidationState(input));
        input.value = formatRUT(input.value);
        updateRutValidationState(input);
    });
}

