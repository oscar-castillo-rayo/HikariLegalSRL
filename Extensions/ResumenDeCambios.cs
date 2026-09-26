namespace HikariLegalSRL.Extensions
{
    public static class ResumenDeCambios
    {
        public static (string? Anterior, string? Nuevo) Diferencias(params (string Campo, string? Antes, string? Despues)[] campos)
        {
            var cambiados = campos
                .Where(c => (c.Antes ?? string.Empty) != (c.Despues ?? string.Empty))
                .ToList();

            if (cambiados.Count == 0)
                return (null, null);

            return (
                string.Join("; ", cambiados.Select(c => $"{c.Campo}: {Mostrar(c.Antes)}")),
                string.Join("; ", cambiados.Select(c => $"{c.Campo}: {Mostrar(c.Despues)}")));
        }

        private static string Mostrar(string? valor) => string.IsNullOrWhiteSpace(valor) ? "(vacío)" : valor;
    }
}
