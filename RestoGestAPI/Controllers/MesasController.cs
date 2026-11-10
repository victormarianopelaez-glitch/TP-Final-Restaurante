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
    public class MesasController : ControllerBase
    {
        private readonly DatabaseContext _context;

        public MesasController(DatabaseContext context)
        {
            _context = context;
        }

        // GET: api/mesas
        [HttpGet]
        public async Task<IActionResult> GetMesas()
        {
            var mesas = await _context.Mesas
                .Where(m => m.Activo)
                .OrderBy(m => m.Numero)
                .ToListAsync();

            var pedidosAbiertos = await _context.Pedidos
                .Include(p => p.Mozo)
                .Include(p => p.Detalles)
                .Where(p => p.Estado == "Abierto")
                .ToListAsync();

            var resultado = mesas.Select(m =>
            {
                var pedidoActivo = pedidosAbiertos.FirstOrDefault(p => p.IdMesa == m.IdMesa);
                return new
                {
                    m.IdMesa,
                    m.Numero,
                    m.Capacidad,
                    m.Estado,
                    PedidoAbierto = pedidoActivo == null ? null : new
                    {
                        pedidoActivo.IdPedido,
                        pedidoActivo.FechaApertura,
                        MinutosOcupada = (int)Math.Max(0, (DateTime.Now - pedidoActivo.FechaApertura).TotalMinutes),
                        MozoNombre = pedidoActivo.Mozo != null ? $"{pedidoActivo.Mozo.Nombre} {pedidoActivo.Mozo.Apellido}" : "",
                        Subtotal = pedidoActivo.Detalles.Sum(d => d.Cantidad * d.PrecioHistorico)
                    }
                };
            });

            return Ok(resultado);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CrearMesa([FromBody] Mesa dto)
        {
            if (dto.Numero <= 0 || dto.Capacidad <= 0)
                return BadRequest(new { mensaje = "Número y capacidad de mesa deben ser mayores a 0." });

            var existe = await _context.Mesas.AnyAsync(m => m.Numero == dto.Numero && m.Activo);
            if (existe)
                return BadRequest(new { mensaje = $"Ya existe la mesa número {dto.Numero}." });

            dto.IdMesa = 0;
            dto.Estado = "Libre";
            dto.Activo = true;
            _context.Mesas.Add(dto);
            await _context.SaveChangesAsync();
            return Ok(dto);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EditarMesa(int id, [FromBody] Mesa dto)
        {
            var mesa = await _context.Mesas.FindAsync(id);
            if (mesa == null || !mesa.Activo)
                return NotFound(new { mensaje = "Mesa no encontrada." });

            mesa.Numero = dto.Numero;
            mesa.Capacidad = dto.Capacidad;
            await _context.SaveChangesAsync();
            return Ok(mesa);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EliminarMesa(int id)
        {
            var mesa = await _context.Mesas.FindAsync(id);
            if (mesa == null || !mesa.Activo)
                return NotFound(new { mensaje = "Mesa no encontrada." });

            if (mesa.Estado == "Ocupada")
                return BadRequest(new { mensaje = "No se puede dar de baja una mesa ocupada." });

            mesa.Activo = false;
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Mesa dada de baja correctamente." });
        }
    }
}
