document.getElementById('periodo').addEventListener('change', function () {
    var partes = new Intl.DateTimeFormat('en-CA', {
        timeZone: 'America/Costa_Rica',
        year: 'numeric',
        month: '2-digit',
        day: '2-digit'
    }).format(new Date()).split('-');

    var anio = parseInt(partes[0], 10);
    var mes = parseInt(partes[1], 10) - 1;
    var dia = parseInt(partes[2], 10);

    var hasta = new Date(Date.UTC(anio, mes, dia));
    var desde;

    switch (this.value) {
        case 'semanal':
            desde = new Date(Date.UTC(anio, mes, dia - 6));
            break;
        case 'cuatrimestral':
            var diasDelMes = new Date(Date.UTC(anio, mes - 3, 0)).getUTCDate();
            desde = new Date(Date.UTC(anio, mes - 4, Math.min(dia, diasDelMes)));
            break;
        default:
            desde = new Date(Date.UTC(anio, mes, 1));
            break;
    }

    var aIso = function (d) { return d.toISOString().slice(0, 10); };
    document.getElementById('desde').value = aIso(desde);
    document.getElementById('hasta').value = aIso(hasta);
});
