(function () {
    const form = document.querySelector('[data-prospecto-direccion]');
    if (!form) return;

    const paisBaseId = parseInt(form.dataset.paisBase || '1', 10);
    const provinciaActual = form.dataset.provinciaActual || '';
    const cantonActual = form.dataset.cantonActual || '';
    const distritoActual = form.dataset.distritoActual || '';

    const chkNacional = document.getElementById('esNacional');
    const bloqueNacional = document.getElementById('bloqueNacional');
    const bloqueExtranjero = document.getElementById('bloqueExtranjero');
    const inputPais = document.getElementById('paisIdOculto');
    const selProvincia = document.getElementById('selectProvincia');
    const selCanton = document.getElementById('selectCanton');
    const selDistrito = document.getElementById('selectDistrito');
    const selPaisExtranjero = document.getElementById('selectPaisExtranjero');
    const hidProvincia = document.getElementById('provinciaIdActual');
    const hidCanton = document.getElementById('cantonIdActual');

    const placeholder = (txt) => '<option value="">' + txt + '</option>';

    async function cargarCantones(provinciaId, preseleccion) {
        selCanton.innerHTML = placeholder('-- Seleccione --');
        selDistrito.innerHTML = placeholder('-- Seleccione un cantón primero --');
        selDistrito.disabled = true;

        if (!provinciaId) {
            selCanton.disabled = true;
            return;
        }

        const respuesta = await fetch('/Prospectos/ObtenerCantones?provinciaId=' + provinciaId);
        const cantones = await respuesta.json();
        cantones.forEach(c => selCanton.add(new Option(c.nombre, c.id)));
        selCanton.disabled = false;

        if (preseleccion) {
            selCanton.value = preseleccion;
            await cargarDistritos(preseleccion, distritoActual);
        }
    }

    async function cargarDistritos(cantonId, preseleccion) {
        selDistrito.innerHTML = placeholder('-- Seleccione --');

        if (!cantonId) {
            selDistrito.disabled = true;
            return;
        }

        const respuesta = await fetch('/Prospectos/ObtenerDistritos?cantonId=' + cantonId);
        const distritos = await respuesta.json();
        distritos.forEach(d => selDistrito.add(new Option(d.nombre, d.id)));
        selDistrito.disabled = false;

        if (preseleccion) selDistrito.value = preseleccion;
    }

    function aplicarModo(esNacional) {
        bloqueNacional.style.display = esNacional ? 'block' : 'none';
        bloqueExtranjero.style.display = esNacional ? 'none' : 'block';

        if (esNacional) {
            inputPais.value = paisBaseId;
            if (!selProvincia.value) {
                selCanton.disabled = true;
                selDistrito.disabled = true;
            }
        } else {
            inputPais.value = selPaisExtranjero.value || '';
            selProvincia.value = '';
            if (hidProvincia) hidProvincia.value = '';
            if (hidCanton) hidCanton.value = '';
            selCanton.innerHTML = placeholder('-- Seleccione una provincia primero --');
            selCanton.disabled = true;
            selDistrito.innerHTML = placeholder('-- Seleccione un cantón primero --');
            selDistrito.disabled = true;
        }
    }

    chkNacional.addEventListener('change', function () {
        aplicarModo(this.checked);
    });

    selPaisExtranjero.addEventListener('change', function () {
        if (parseInt(this.value, 10) === paisBaseId) {
            chkNacional.checked = true;
            chkNacional.dispatchEvent(new Event('change'));
            return;
        }
        inputPais.value = this.value;
    });

    selProvincia.addEventListener('change', function () {
        if (hidProvincia) hidProvincia.value = this.value;
        if (hidCanton) hidCanton.value = '';
        cargarCantones(this.value);
    });

    selCanton.addEventListener('change', function () {
        if (hidCanton) hidCanton.value = this.value;
        cargarDistritos(this.value);
    });

    form.addEventListener('submit', function () {
        if (!selDistrito.value || selDistrito.value === '0') selDistrito.disabled = true;
    });

    if (chkNacional.checked) {
        inputPais.value = paisBaseId;
        if (provinciaActual) {
            selProvincia.value = provinciaActual;
            cargarCantones(provinciaActual, cantonActual);
        }
    } else {
        if (inputPais.value) selPaisExtranjero.value = inputPais.value;
        bloqueNacional.style.display = 'none';
        bloqueExtranjero.style.display = 'block';
        selCanton.disabled = true;
        selDistrito.disabled = true;
    }
})();
