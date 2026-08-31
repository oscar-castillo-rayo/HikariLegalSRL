using HikariLegalSRL.Data;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Services.Implementations
{
    public class GeografiaService : IGeografiaService
    {
        private readonly ApplicationDbContext _context;

        public GeografiaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<OpcionComboDTO>> ObtenerProvincias()
        {
            return await _context.Provincias
                .OrderBy(p => p.Nombre)
                .Select(p => new OpcionComboDTO { Id = p.ProvinciaId, Nombre = p.Nombre })
                .ToListAsync();
        }

        public async Task<List<OpcionComboDTO>> ObtenerCantones(int provinciaId)
        {
            return await _context.Cantones
                .Where(c => c.ProvinciaId == provinciaId)
                .OrderBy(p => p.Nombre)
                .Select(c => new OpcionComboDTO { Id = c.CantonId, Nombre = c.Nombre })
                .ToListAsync();
        }

        public async Task<List<OpcionComboDTO>> ObtenerDistritos(int cantonId)
        {
            return await _context.Distritos
                .Where(d => d.CantonId == cantonId)
                .OrderBy(d => d.Nombre)
                .Select(d => new OpcionComboDTO { Id = d.DistritoId, Nombre = d.Nombre })
                .ToListAsync();
        }

        public async Task<List<OpcionComboDTO>> ObtenerPaises()
        {
            return await _context.Paises
                 .OrderBy(p => p.Nombre)
                 .Select(p => new OpcionComboDTO { Id = p.PaisId, Nombre = p.Nombre })
                 .ToListAsync();
        }
    }
}
