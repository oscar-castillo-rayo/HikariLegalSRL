document.getElementById('periodo').addEventListener('change', function () {
    var hoy = new Date();
    var hasta = new Date(hoy);
    var desde = new Date(hoy);

    switch (this.value) {
        case 'semanal':
            desde.setDate(hoy.getDate() - 6);
            break;
        case 'cuatrimestral':
            desde.setMonth(hoy.getMonth() - 4);
            break;
        default:
            desde = new Date(hoy.getFullYear(), hoy.getMonth(), 1);
            break;
    }

    var aIso = function (d) { return d.toISOString().slice(0, 10); };
    document.getElementById('desde').value = aIso(desde);
    document.getElementById('hasta').value = aIso(hasta);
});
