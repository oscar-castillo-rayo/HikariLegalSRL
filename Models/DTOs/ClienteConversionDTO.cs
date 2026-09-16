using System.ComponentModel.DataAnnotations;
using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class ClienteConversionDTO
    {
        [Required(ErrorMessage = "Debe seleccionar la modalidad de pago")]
        public ModalidadPago? ModalidadPago { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un Abogado/Asesor responsable")]
        public string? ResponsableId { get; set; }
    }
}
