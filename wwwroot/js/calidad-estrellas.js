(function () {
    var radios = document.querySelectorAll('.calidad-estrella-radio');
    var etiquetas = document.querySelectorAll('.calidad-estrella-label');

    function pintar(cantidad) {
        etiquetas.forEach(function (etiqueta, indice) {
            var activa = indice < cantidad;
            etiqueta.classList.toggle('activa', activa);
            var icono = etiqueta.querySelector('i');
            icono.classList.toggle('bi-star-fill', activa);
            icono.classList.toggle('bi-star', !activa);
        });
    }

    function seleccionada() {
        var marcada = document.querySelector('.calidad-estrella-radio:checked');
        return marcada ? parseInt(marcada.value, 10) : 0;
    }

    etiquetas.forEach(function (etiqueta, indice) {
        etiqueta.addEventListener('mouseenter', function () { pintar(indice + 1); });
        etiqueta.addEventListener('mouseleave', function () { pintar(seleccionada()); });
    });

    radios.forEach(function (radio) {
        radio.addEventListener('change', function () { pintar(seleccionada()); });
    });

    pintar(seleccionada());
})();
