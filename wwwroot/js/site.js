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

});