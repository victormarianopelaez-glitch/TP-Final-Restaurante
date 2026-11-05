using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestoGestAPI.Data;
using RestoGestAPI.Models;

namespace RestoGestAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly DatabaseContext _context;
        private readonly IConfiguration _config;

        public AuthController(DatabaseContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public record LoginRequest(string Email, string Password);

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { mensaje = "Debe ingresar email y contraseña." });

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == req.Email && u.Activo);

            if (usuario == null || !VerifyPassword(req.Password, usuario.Password))
                return Unauthorized(new { mensaje = "Credenciales invalidas o usuario inactivo." });

            var token = GenerateJwtToken(usuario);

            return Ok(new
            {
                token,
                usuario = new
                {
                    usuario.IdUsuario,
                    usuario.Nombre,
                    usuario.Apellido,
                    usuario.Email,
                    usuario.Telefono,
                    usuario.Rol,
                    usuario.AvatarRuta
                }
            });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idClaim, out int idUsuario))
                return Unauthorized();

            var usuario = await _context.Usuarios.FindAsync(idUsuario);
            if (usuario == null || !usuario.Activo)
                return NotFound();

            return Ok(new
            {
                usuario.IdUsuario,
                usuario.Nombre,
                usuario.Apellido,
                usuario.Email,
                usuario.Telefono,
                usuario.Rol,
                usuario.AvatarRuta
            });
        }

        public static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        public static bool VerifyPassword(string inputPassword, string storedPassword)
        {
            if (storedPassword == inputPassword) return true;
            return storedPassword == HashPassword(inputPassword);
        }

        private string GenerateJwtToken(Usuario usuario)
        {
            var jwtKey = _config["Jwt:Key"] ?? "ClaveSecretaSuperSeguraParaRestoGest2026_Laboratorio2_UNSL!";
            var jwtIssuer = _config["Jwt:Issuer"] ?? "RestoGestAPI";
            var jwtAudience = _config["Jwt:Audience"] ?? "RestoGestFrontend";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Name, $"{usuario.Nombre} {usuario.Apellido}"),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
