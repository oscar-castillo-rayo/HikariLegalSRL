(function () {
    const form = document.getElementById('formPropuesta');
    const selectProspecto = document.getElementById('selectProspecto');
    const selectCliente = document.getElementById('selectCliente');
    const cuerpoItems = document.getElementById('cuerpoItems');
    const btnAgregarServicio = document.getElementById('btnAgregarServicio');
    const totalEl = document.getElementById('totalPropuesta');
    const templateServicios = document.getElementById('templateServicios');

    if (!cuerpoItems || !btnAgregarServicio || !templateServicios) return;

    function alternarDestinatario() {
        if (selectProspecto.value) {
            selectCliente.value = '';
            selectCliente.disabled = true;
        } else if (selectCliente.value) {
            selectProspecto.value = '';
            selectProspecto.disabled = true;
        } else {
            selectProspecto.disabled = false;
            selectCliente.disabled = false;
        }
    }

    if (selectProspecto && selectCliente) {
        selectProspecto.addEventListener('change', alternarDestinatario);
        selectCliente.addEventListener('change', alternarDestinatario);
        alternarDestinatario();
    }

    let itemIndex = 0;

    function recalcularTotal() {
        const precios = Array.from(cuerpoItems.querySelectorAll('.inp-precio'))
            .map(i => parseFloat(i.value) || 0);
        const total = precios.reduce((a, b) => a + b, 0);
        totalEl.textContent = total.toLocaleString('es-CR', { minimumFractionDigits: 2 });
    }

    function agregarFila(datosIniciales) {
        const idx = itemIndex++;

        const opciones = Array.from(templateServicios.options)
            .map(o => {
                const seleccionado = datosIniciales && String(datosIniciales.servicioId) === o.value ? 'selected' : '';
                return `<option value="${o.value}" data-precio="${o.dataset.precio}" ${seleccionado}>${o.text}</option>`;
            })
            .join('');

        const descripcion = datosIniciales?.descripcion ?? '';
        const precio = datosIniciales?.precio ?? '';

        const fila = document.createElement('tr');
        fila.innerHTML = `
            <td style="min-width:200px;">
                <select name="Propuesta.Servicios[${idx}].ServicioId" class="form-select form-select-sm sel-servicio" required>
                    <option value="">-- Seleccione --</option>
                    ${opciones}
                </select>
            </td>
            <td>
                <input type="text" name="Propuesta.Servicios[${idx}].DescripcionServicio"
                       class="form-control form-control-sm" placeholder="Opcional..." value="${descripcion}" />
            </td>
            <td style="min-width:140px;">
                <input type="number" name="Propuesta.Servicios[${idx}].Precio"
                       class="form-control form-control-sm inp-precio"
                       min="0" step="0.01" required placeholder="0" value="${precio}" />
            </td>
            <td class="text-center">
                <button type="button" class="btn btn-sm btn-outline-danger btn-eliminar-servicio">
                    <i class="bi bi-trash"></i>
                </button>
            </td>`;

        fila.querySelector('.sel-servicio').addEventListener('change', function () {
            const opcionSeleccionada = this.selectedOptions[0];
            const precioInput = fila.querySelector('.inp-precio');
            if (opcionSeleccionada && opcionSeleccionada.dataset.precio) {
                precioInput.value = opcionSeleccionada.dataset.precio;
                recalcularTotal();
            }
        });

        fila.querySelector('.inp-precio').addEventListener('input', recalcularTotal);

        fila.querySelector('.btn-eliminar-servicio').addEventListener('click', function () {
            fila.remove();
            recalcularTotal();
        });

        cuerpoItems.appendChild(fila);
    }

    btnAgregarServicio.addEventListener('click', () => agregarFila());

    const iniciales = window.propuestaServiciosIniciales;
    if (Array.isArray(iniciales) && iniciales.length > 0) {
        iniciales.forEach(agregarFila);
        recalcularTotal();
    } else {
        agregarFila();
    }

    if (form) {
        form.addEventListener('submit', function (e) {
            if (cuerpoItems.children.length === 0) {
                e.preventDefault();
                if (window.Swal) {
                    window.Swal.fire({
                        icon: 'error',
                        title: 'Falta un servicio',
                        text: 'Debe agregar al menos un servicio a la propuesta.'
                    });
                } else {
                    alert('Debe agregar al menos un servicio a la propuesta.');
                }
            }
        });
    }
})();
