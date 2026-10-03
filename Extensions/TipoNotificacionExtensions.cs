using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Extensions
{
    public static class TipoNotificacionExtensions
    {
        // Rojo = vencido/urgente, amarillo = próximo a vencer, azul/info = evento sin
        // urgencia de tiempo (criterio documentado en .claude/notas/notas.md, sección Notificaciones).
        public static (string ColorClase, string Icono) EstiloVisual(this TipoNotificacion tipo) => tipo switch
        {
            TipoNotificacion.AlertaTareaVencida => ("danger", "bi-exclamation-triangle-fill"),
            TipoNotificacion.EscalamientoAdminTarea => ("danger", "bi-exclamation-octagon-fill"),
            TipoNotificacion.EscalamientoAdminExpediente => ("danger", "bi-exclamation-octagon-fill"),
            TipoNotificacion.AlertaTareaProxima => ("warning", "bi-clock-history"),
            TipoNotificacion.AlertaExpedienteProximo => ("warning", "bi-folder2-open"),
            TipoNotificacion.AlertaExpedienteVencido => ("danger", "bi-calendar-x-fill"),
            TipoNotificacion.AlertaSeguimientoProximo => ("warning", "bi-calendar-event"),
            TipoNotificacion.ReasignacionExpediente => ("info", "bi-person-gear"),
            TipoNotificacion.ReasignacionTarea => ("info", "bi-person-check-fill"),
            TipoNotificacion.CambioEstadoTarea => ("info", "bi-arrow-repeat"),
            _ => ("info", "bi-arrow-repeat")
        };
    }
}
