(function () {
    const form = document.getElementById('formNuevaTarea');
    if (!form) return;

    const plazoComprometido = form.dataset.plazoComprometido;
    const inputFecha = document.getElementById('nuevaTareaFechaLimite');
    let confirmado = false;

    form.addEventListener('submit', function (e) {
        if (confirmado) return;
        if (!inputFecha.value || !plazoComprometido || inputFecha.value <= plazoComprometido) return;

        e.preventDefault();
        Swal.fire({
            title: 'La fecha límite supera el plazo del expediente',
            text: 'El plazo comprometido con el cliente es el ' + plazoComprometido + '. ¿Desea continuar de todas formas?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#dc3545',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Sí, continuar',
            cancelButtonText: 'Cancelar'
        }).then((result) => {
            if (result.isConfirmed) {
                confirmado = true;
                form.submit();
            }
        });
    });
})();
