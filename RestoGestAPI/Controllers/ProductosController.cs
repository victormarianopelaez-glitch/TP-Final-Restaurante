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
    public class ProductosController : ControllerBase
    {
        private readonly DatabaseContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductosController(DatabaseContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: api/productos?page=1&pageSize=8&search=&idCategoria=&estado=

        [HttpGet]
        public async Task<IActionResult> GetProductos(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 8,
            [FromQuery] string? search = null,
            [FromQuery] int? idCategoria = null,
            [FromQuery] string? estado = null)
        {
            var query = _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Activo)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Nombre.Contains(search) || (p.Descripcion != null && p.Descripcion.Contains(search)));

            if (idCategoria.HasValue && idCategoria.Value > 0)
                query = query.Where(p => p.IdCategoria == idCategoria.Value);

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(p => p.Estado == estado);

            var totalItems = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Categoria!.Nombre)
                .ThenBy(p => p.Nombre)
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

        // GET: api/productos/buscar?q=milanesa&idCategoria=2

        [HttpGet("buscar")]
        public async Task<IActionResult> BuscarProductosDisponibles([FromQuery] string? q = null, [FromQuery] int? idCategoria = null)
        {
            var query = _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Activo && p.Estado == "Disponible")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(p => p.Nombre.Contains(q) || (p.Descripcion != null && p.Descripcion.Contains(q)));
            }

            if (idCategoria.HasValue && idCategoria.Value > 0)
            {
                query = query.Where(p => p.IdCategoria == idCategoria.Value);
            }

            var resultados = await query
                .OrderBy(p => p.Nombre)
                .Take(15)
                .Select(p => new
                {
                    p.IdProducto,
                    p.Nombre,
                    p.Descripcion,
                    p.PrecioVigente,
                    p.ImagenRuta,
                    p.IdCategoria,
                    CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : ""
                })
                .ToListAsync();

            return Ok(resultados);
        }

        public class ProductoFormDto
        {
            public int IdCategoria { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string? Descripcion { get; set; }
            public decimal PrecioVigente { get; set; }
            public string Estado { get; set; } = "Disponible";
            public IFormFile? Imagen { get; set; }
        }

        // POST: api/productos (Multipart/form-data con archivo de imagen)
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CrearProducto([FromForm] ProductoFormDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre) || dto.PrecioVigente < 0)
                return BadRequest(new { mensaje = "Nombre y precio válido son obligatorios." });

            string? rutaImagen = null;
            if (dto.Imagen != null && dto.Imagen.Length > 0)
            {
                rutaImagen = await GuardarImagenProducto(dto.Imagen);
            }

            var nuevo = new Producto
            {
                IdCategoria = dto.IdCategoria,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                PrecioVigente = dto.PrecioVigente,
                ImagenRuta = rutaImagen,
                Estado = dto.Estado == "Sin stock" ? "Sin stock" : "Disponible",
                Activo = true
            };

            _context.Productos.Add(nuevo);
            await _context.SaveChangesAsync();
            return Ok(nuevo);
        }

        // PUT: api/productos/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EditarProducto(int id, [FromForm] ProductoFormDto dto)
        {
            var prod = await _context.Productos.FindAsync(id);
            if (prod == null || !prod.Activo)
                return NotFound(new { mensaje = "Producto no encontrado." });

            prod.IdCategoria = dto.IdCategoria;
            prod.Nombre = dto.Nombre;
            prod.Descripcion = dto.Descripcion;
            prod.PrecioVigente = dto.PrecioVigente;
            prod.Estado = dto.Estado == "Sin stock" ? "Sin stock" : "Disponible";

            if (dto.Imagen != null && dto.Imagen.Length > 0)
            {
                prod.ImagenRuta = await GuardarImagenProducto(dto.Imagen);
            }

            await _context.SaveChangesAsync();
            return Ok(prod);
        }

        public record CambiarEstadoDto(string Estado);

        // PATCH: api/productos/{id}/estado (Suspender temporalmente por falta de stock o reactivar)
        [HttpPatch("{id}/estado")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CambiarDisponibilidad(int id, [FromBody] CambiarEstadoDto dto)
        {
            var prod = await _context.Productos.FindAsync(id);
            if (prod == null || !prod.Activo)
                return NotFound(new { mensaje = "Producto no encontrado." });

            prod.Estado = dto.Estado == "Sin stock" ? "Sin stock" : "Disponible";
            await _context.SaveChangesAsync();
            return Ok(prod);
        }

        // DELETE: api/productos/{id} (Baja lógica)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EliminarProducto(int id)
        {
            var prod = await _context.Productos.FindAsync(id);
            if (prod == null || !prod.Activo)
                return NotFound(new { mensaje = "Producto no encontrado." });

            prod.Activo = false;
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Producto eliminado lógicamente del catálogo." });
        }

        private async Task<string> GuardarImagenProducto(IFormFile archivo)
        {
            var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            var permitidas = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            if (!permitidas.Contains(ext)) ext = ".png";

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var carpeta = Path.Combine(webRoot, "uploads", "productos");
            Directory.CreateDirectory(carpeta);

            var nombreArchivo = $"prod_{Guid.NewGuid():N}{ext}";
            var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            return $"/uploads/productos/{nombreArchivo}";
        }
    }
}
