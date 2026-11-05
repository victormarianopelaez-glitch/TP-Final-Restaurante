using System.Security.Claims;
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
    public class UsuariosController : ControllerBase
    {
        private readonly DatabaseContext _context;
        private readonly IWebHostEnvironment _env;

        public UsuariosController(DatabaseContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: api/usuarios
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetUsuarios([FromQuery] int page = 1, [FromQuery] int pageSize = 8, [FromQuery] string? search = null)
        {
            var query = _context.Usuarios.Where(u => u.Activo).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.Nombre.Contains(search) || u.Apellido.Contains(search) || u.Email.Contains(search));
            }

            var totalItems = await query.CountAsync();
            var items = await query
                .OrderBy(u => u.Apellido)
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

        public record CrearUsuarioDto(string Nombre, string Apellido, string? Telefono, string Email, string Password, string Rol);

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CrearUsuario([FromBody] CrearUsuarioDto dto)
        {
            if (await _context.Usuarios.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { mensaje = "El email ya está registrado." });

            var nuevo = new Usuario
            {
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                Telefono = dto.Telefono,
                Email = dto.Email,
                Password = AuthController.HashPassword(dto.Password),
                Rol = dto.Rol == "Administrador" ? "Administrador" : "Mozo",
                Activo = true
            };

            _context.Usuarios.Add(nuevo);
            await _context.SaveChangesAsync();
            return Ok(nuevo);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EditarUsuario(int id, [FromBody] CrearUsuarioDto dto)
        {
            var u = await _context.Usuarios.FindAsync(id);
            if (u == null || !u.Activo)
                return NotFound(new { mensaje = "Usuario no encontrado." });

            u.Nombre = dto.Nombre;
            u.Apellido = dto.Apellido;
            u.Telefono = dto.Telefono;
            u.Email = dto.Email;
            u.Rol = dto.Rol == "Administrador" ? "Administrador" : "Mozo";

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                u.Password = AuthController.HashPassword(dto.Password);
            }

            await _context.SaveChangesAsync();
            return Ok(u);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            var u = await _context.Usuarios.FindAsync(id);
            if (u == null || !u.Activo)
                return NotFound(new { mensaje = "Usuario no encontrado." });

            u.Activo = false;
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Usuario dado de baja lógicamente." });
        }

        public class ActualizarPerfilFormDto
        {
            public string Nombre { get; set; } = string.Empty;
            public string Apellido { get; set; } = string.Empty;
            public string? Telefono { get; set; }
            public string? NuevaPassword { get; set; }
            public IFormFile? Avatar { get; set; }
        }

        // PUT: api/usuarios/perfil (Tanto Administrador como Mozo pueden actualizar su perfil y Avatar)
        [HttpPut("perfil")]
        public async Task<IActionResult> ActualizarMiPerfil([FromForm] ActualizarPerfilFormDto dto)
        {
            int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var u = await _context.Usuarios.FindAsync(idUsuario);
            if (u == null || !u.Activo)
                return NotFound(new { mensaje = "Usuario no encontrado." });

            if (!string.IsNullOrWhiteSpace(dto.Nombre)) u.Nombre = dto.Nombre;
            if (!string.IsNullOrWhiteSpace(dto.Apellido)) u.Apellido = dto.Apellido;
            u.Telefono = dto.Telefono;

            if (!string.IsNullOrWhiteSpace(dto.NuevaPassword))
            {
                u.Password = AuthController.HashPassword(dto.NuevaPassword);
            }

            if (dto.Avatar != null && dto.Avatar.Length > 0)
            {
                var ext = Path.GetExtension(dto.Avatar.FileName).ToLowerInvariant();
                var permitidas = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                if (!permitidas.Contains(ext)) ext = ".png";

                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var carpeta = Path.Combine(webRoot, "uploads", "avatars");
                Directory.CreateDirectory(carpeta);

                var nombreArchivo = $"avatar_{u.IdUsuario}_{Guid.NewGuid():N}{ext}";
                var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    await dto.Avatar.CopyToAsync(stream);
                }

                u.AvatarRuta = $"/uploads/avatars/{nombreArchivo}";
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                u.IdUsuario,
                u.Nombre,
                u.Apellido,
                u.Email,
                u.Telefono,
                u.Rol,
                u.AvatarRuta
            });
        }
    }
}
