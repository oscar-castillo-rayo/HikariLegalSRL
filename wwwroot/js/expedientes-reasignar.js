(function () {
    const modalEl = document.getElementById('modalReasignar');
    if (!modalEl) return;

    const boton = document.getElementById('botonNuevoResponsable');
    const texto = document.getElementById('textoNuevoResponsable');
    const input = document.getElementById('selectNuevoResponsable');
    const menu = modalEl.querySelector('.expedientes-dropdown-menu');
    const mensajeError = modalEl.querySelector('[data-valmsg-for="ResponsableId"]');
    const form = modalEl.querySelector('form');

    function limpiar() {
        input.value = '';
        texto.textContent = '-- Seleccione --';
        boton.classList.remove('input-validation-error');
        boton.classList.add('input-validation-valid');
        if (mensajeError) {
            mensajeError.textContent = '';
            mensajeError.classList.remove('field-validation-error');
            mensajeError.classList.add('field-validation-valid');
        }
        if (window.jQuery && form) {
            try {
                window.jQuery(form).validate().resetForm();
            } catch (err) {
                console.warn('No se pudo reiniciar jQuery Validate:', err);
            }
        }
    }

    menu.addEventListener('click', function (e) {
        const item = e.target.closest('.dropdown-item');
        if (!item) return;
        e.preventDefault();

        input.value = item.dataset.id;
        texto.textContent = item.dataset.nombre;

        if (window.jQuery && form) {
            window.jQuery(input).valid();
        }
    });

    modalEl.addEventListener('hidden.bs.modal', limpiar);
})();
