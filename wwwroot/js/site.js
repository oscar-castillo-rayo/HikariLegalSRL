document.addEventListener('DOMContentLoaded', function () {

    document.addEventListener('submit', function (e) {
        // Verificamos si el formulario que dispara el evento tiene la clase
        if (e.target && e.target.classList.contains('form-confirmar')) {

            e.preventDefault();

            const form = e.target;
            const titulo = form.getAttribute('data-titulo') || '¿Estás seguro?';
            const texto = form.getAttribute('data-texto') || 'Esta acción no se puede deshacer.';
            const icono = form.getAttribute('data-icono') || 'warning';
            const textoBoton = form.getAttribute('data-btn-confirmar') || 'Sí, continuar';
            const colorBoton = icono === 'info' ? '#198754' : '#dc3545';

            Swal.fire({
                title: titulo,
                text: texto,
                icon: icono,
                showCancelButton: true,
                confirmButtonColor: colorBoton,
                cancelButtonColor: '#6c757d',
                confirmButtonText: textoBoton,
                cancelButtonText: 'Cancelar'
            }).then((result) => {
                if (result.isConfirmed) {
                    form.submit();
                }
            });
        }
    });

    document.querySelectorAll('i.bi').forEach(function (icono) {
        if (!icono.hasAttribute('aria-label')) {
            icono.setAttribute('aria-hidden', 'true');
        }
    });

    document.querySelectorAll('a[title], button[title], label[title]').forEach(function (control) {
        if (!control.hasAttribute('aria-label') && !control.textContent.trim()) {
            control.setAttribute('aria-label', control.getAttribute('title'));
        }
    });

    document.querySelectorAll('label:not([for])').forEach(function (etiqueta) {
        if (etiqueta.querySelector('input, select, textarea')) {
            return;
        }

        const selector = 'input:not([type="hidden"]), select, textarea';
        let control = null;
        let hermano = etiqueta.nextElementSibling;
        while (hermano && !control && hermano.tagName !== 'LABEL') {
            control = hermano.matches(selector) ? hermano : hermano.querySelector(selector);
            hermano = hermano.nextElementSibling;
        }

        if (!control) {
            return;
        }
        if (!control.id) {
            control.id = 'campo-' + Math.random().toString(36).slice(2, 9);
        }
        etiqueta.setAttribute('for', control.id);
    });

    document.querySelectorAll('[data-val-required], [required]').forEach(function (campo) {
        if (!campo.id || campo.type === 'hidden' || campo.type === 'checkbox' || campo.type === 'radio') {
            return;
        }
        const etiqueta = document.querySelector('label[for="' + campo.id + '"]');
        if (etiqueta) {
            etiqueta.classList.add('etiqueta-requerida');
        }
    });

    document.querySelectorAll('table').forEach(function (tabla) {
        const encabezados = Array.from(tabla.querySelectorAll('thead th'));
        if (!encabezados.some(function (th) { return ['Acciones', 'Detalle'].includes(th.textContent.trim()); })) {
            return;
        }

        tabla.classList.add('tabla-apilable');
        tabla.querySelectorAll('tbody tr').forEach(function (fila) {
            Array.from(fila.children).forEach(function (celda, i) {
                if (celda.colSpan > 1 || !encabezados[i]) {
                    return;
                }
                celda.setAttribute('data-label', encabezados[i].textContent.trim());

                if (celda !== fila.firstElementChild && celda !== fila.lastElementChild && celda.childNodes.length > 1) {
                    const valor = document.createElement('div');
                    while (celda.firstChild) {
                        valor.appendChild(celda.firstChild);
                    }
                    celda.appendChild(valor);
                }
            });
        });
    });

    document.querySelectorAll('#sidebarOffcanvas .nav-link.active').forEach(function (enlace) {
        enlace.setAttribute('aria-current', 'page');
    });

    document.addEventListener('submit', function (e) {
        const form = e.target;
        if (!(form instanceof HTMLFormElement) || form.classList.contains('form-confirmar') || e.defaultPrevented) {
            return;
        }
        if ((form.getAttribute('method') || 'get').toLowerCase() !== 'post') {
            return;
        }

        form.querySelectorAll('button[type="submit"]').forEach(function (boton) {
            if (boton === e.submitter) {
                boton.insertAdjacentHTML('afterbegin', '<span class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span>');
            }
            boton.disabled = true;
            boton.setAttribute('aria-busy', 'true');
        });
    });

    window.addEventListener('pageshow', function (e) {
        if (!e.persisted) {
            return;
        }
        document.querySelectorAll('button[aria-busy="true"]').forEach(function (boton) {
            boton.disabled = false;
            boton.removeAttribute('aria-busy');
            const spinner = boton.querySelector('.spinner-border');
            if (spinner) {
                spinner.remove();
            }
        });
    });

    const mensajesNativos = function (campo) {
        const v = campo.validity;
        if (v.valueMissing) {
            if (campo.type === 'checkbox') return 'Debe marcar esta casilla para continuar.';
            if (campo.tagName === 'SELECT' || campo.type === 'radio') return 'Seleccione una opción de la lista.';
            if (campo.type === 'file') return 'Seleccione un archivo.';
            return 'Complete este campo.';
        }
        if (v.typeMismatch) {
            if (campo.type === 'email') return 'Ingrese un correo electrónico válido.';
            if (campo.type === 'url') return 'Ingrese una URL válida.';
            return 'El valor ingresado no es válido.';
        }
        if (v.badInput) return 'Ingrese un valor válido.';
        if (v.rangeUnderflow) return 'El valor debe ser mayor o igual a ' + campo.min + '.';
        if (v.rangeOverflow) return 'El valor debe ser menor o igual a ' + campo.max + '.';
        if (v.stepMismatch) return 'Ingrese un valor válido; el paso permitido es ' + campo.step + '.';
        if (v.tooShort) return 'Ingrese al menos ' + campo.minLength + ' caracteres.';
        if (v.tooLong) return 'No ingrese más de ' + campo.maxLength + ' caracteres.';
        if (v.patternMismatch) return campo.title || 'El valor no tiene el formato solicitado.';
        return '';
    };

    document.addEventListener('invalid', function (e) {
        const campo = e.target;
        if (!campo.setCustomValidity) {
            return;
        }
        campo.setCustomValidity('');
        campo.setCustomValidity(mensajesNativos(campo));
    }, true);

    ['input', 'change'].forEach(function (evento) {
        document.addEventListener(evento, function (e) {
            if (e.target.setCustomValidity) {
                e.target.setCustomValidity('');
            }
        });
    });

});