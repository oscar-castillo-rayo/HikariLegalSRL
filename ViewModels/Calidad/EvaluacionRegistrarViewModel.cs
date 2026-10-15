using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.ViewModels.Calidad
{
    public class EvaluacionRegistrarViewModel
    {
        public ExpedienteEvaluableDTO Expediente { get; set; } = new();
        public EvaluacionCalidadCreacionDTO Evaluacion { get; set; } = new();
    }
}
