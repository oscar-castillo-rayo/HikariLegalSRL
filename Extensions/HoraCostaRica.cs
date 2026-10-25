namespace HikariLegalSRL.Extensions
{
    public static class HoraCostaRica
    {
        private static readonly TimeSpan DesfaseEstandar = TimeSpan.FromHours(-6);

        public static TimeZoneInfo Zona { get; } = ResolverZona();

        public static DateTime Ahora => ALocal(DateTime.UtcNow);

        public static DateTime Hoy => HoyEn(DateTime.UtcNow);

        public static DateTime HoyEn(DateTime instanteUtc) => ALocal(instanteUtc).Date;

        public static DateTime ALocal(this DateTime instanteUtc)
        {
            var utc = DateTime.SpecifyKind(instanteUtc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, Zona);
        }

        public static DateTime? ALocal(this DateTime? instanteUtc)
            => instanteUtc?.ALocal();

        public static DateTime AUtc(this DateTime horaLocal)
        {
            var sinZona = DateTime.SpecifyKind(horaLocal, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(sinZona, Zona);
        }

        public static (DateTime DesdeUtc, DateTime HastaUtc) LimitesUtc(DateTime desde, DateTime hasta)
            => (desde.Date.AUtc(), hasta.Date.AddDays(1).AUtc());

        private static TimeZoneInfo ResolverZona()
        {
            foreach (var id in new[] { "Central America Standard Time", "America/Costa_Rica" })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return TimeZoneInfo.CreateCustomTimeZone("America/Costa_Rica", DesfaseEstandar, "Costa Rica", "Costa Rica");
        }
    }
}
