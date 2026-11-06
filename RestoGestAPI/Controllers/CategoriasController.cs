using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestoGestAPI.Data;
using RestoGestAPI.Models;

namespace RestoGestAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategoriasController : ControllerBase
    {
        private readonly DatabaseContext _context;

        public CategoriasController(DatabaseContext context)
        {
            _context = context;
        }

        // GET: api/categorias?page=1&pageSize=10&search=
        [HttpGet]
        public async Task<IActionResult> GetCategorias([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] bool all = false)
        {
            var query = _context.Categorias.Where(c => c.Activo).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.Nombre.Contains(search) || (c.Descripcion != null && c.Descripcion.Contains(search)));
            }

            if (all)
            {
                var lista = await query.OrderBy(c => c.Nombre).ToListAsync();
                return Ok(lista);
            }

            var totalItems = await query.CountAsync();
            var items = await query
                .OrderBy(c => c.Nombre)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                items,
                totalItems,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            });
        }

        // GET: api/categorias/buscar?q=beb
        [HttpGet("buscar")]
        public async Task<IActionResult> BuscarCategorias([FromQuery] string? q = null)
        {
            var query = _context.Categorias.Where(c => c.Activo);
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(c => c.Nombre.Contains(q));
            }
            var resultados = await query.OrderBy(c => c.Nombre).Take(15).ToListAsync();
            return Ok(resultados);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CrearCategoria([FromBody] Categoria dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return BadRequest(new { mensaje = "El nombre de la categoría es obligatorio." });

            dto.IdCategoria = 0;
            dto.Activo = true;
            _context.Categorias.Add(dto);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCategorias), new { id = dto.IdCategoria }, dto);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EditarCategoria(int id, [FromBody] Categoria dto)
        {
            var cat = await _context.Categorias.FindAsync(id);
            if (cat == null || !cat.Activo)
                return NotFound(new { mensaje = "Categoría no encontrada." });

            cat.Nombre = dto.Nombre;
            cat.Descripcion = dto.Descripcion;
            await _context.SaveChangesAsync();
            return Ok(cat);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EliminarCategoria(int id)
        {
            var cat = await _context.Categorias.FindAsync(id);
            if (cat == null || !cat.Activo)
                return NotFound(new { mensaje = "Categoría no encontrada." });

            // Baja lógica para preservar integridad referencial en MySQL
            cat.Activo = false;
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Categoría dada de baja correctamente." });
        }
    }
}
