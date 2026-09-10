(function () {
    const contenedor = document.querySelector('.prospectos-seg');
    if (!contenedor) return;

    const urlRegistrar = contenedor.dataset.urlRegistrar;
    const urlEditar = contenedor.dataset.urlEditar;

    const modalVerEl = document.getElementById('modalVerActividad');
    const modalFormEl = document.getElementById('modalFormActividad');

    const form = document.getElementById('formActividad');
    const campoTipo = document.getElementById('formActividadTipo');
    const campoTitulo = document.getElementById('formActividadTituloInput');
    const campoFecha = document.getElementById('formActividadFecha');
    const campoResponsable = document.getElementById('formActividadResponsable');
    const campoDescripcion = document.getElementById('formActividadDescripcion');
    const formTitulo = document.getElementById('formActividadTitulo');

    function fechaLocalInput(fecha) {
        const desfase = fecha.getTimezoneOffset() * 60000;
        return new Date(fecha.getTime() - desfase).toISOString().slice(0, 16);
    }

    function abrir(el) {
        bootstrap.Modal.getOrCreateInstance(el).show();
    }

    contenedor.addEventListener('click', function (e) {
        const boton = e.target.closest('[data-accion]');
        if (!boton) return;

        const accion = boton.dataset.accion;
        const item = boton.closest('.prospectos-seg-item');

        if (accion === 'nueva-actividad' && form) {
            form.reset();
            form.setAttribute('action', urlRegistrar);
            formTitulo.textContent = 'Agregar seguimiento';
            campoFecha.value = fechaLocalInput(new Date());
            abrir(modalFormEl);
            return;
        }

        if (accion === 'editar-actividad' && form && item) {
            const d = item.dataset;
            form.setAttribute('action', urlEditar + '?actividadId=' + d.id);
            formTitulo.textContent = 'Editar seguimiento';
            campoTipo.value = d.tipo;
            campoTitulo.value = d.titulo;
            campoFecha.value = d.fecha;
            campoResponsable.value = d.responsableId;
            campoDescripcion.value = d.descripcion || '';
            abrir(modalFormEl);
            return;
        }

        if (accion === 'ver-actividad' && item) {
            const d = item.dataset;
            document.getElementById('verActividadTipo').textContent = d.tipoLabel;
            document.getElementById('verActividadTitulo').textContent = d.titulo;
            document.getElementById('verActividadDescripcion').textContent =
                d.descripcion && d.descripcion.trim() ? d.descripcion : 'Sin descripción.';
            document.getElementById('verActividadRegistro').textContent = d.registradoPor;
            document.getElementById('verActividadResponsable').textContent = d.responsable;
            document.getElementById('verActividadFecha').textContent = d.fechaTexto;
            abrir(modalVerEl);
        }
    });
})();
