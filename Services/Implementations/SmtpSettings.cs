namespace HikariLegalSRL.Services.Implementations
{
    /// <summary>
    /// Opciones de configuración del servidor SMTP para el envío de correos: servidor, puerto, credenciales y remitente.
    /// </summary>
    public class SmtpSettings
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public bool UseSsl { get; set; } = false;
    }
}
