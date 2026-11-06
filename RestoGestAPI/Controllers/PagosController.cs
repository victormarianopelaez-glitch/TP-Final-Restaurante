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
    public class PagosController : ControllerBase
    {
        private readonly DatabaseContext _context;

        public PagosController(DatabaseContext context)
        {
            _context = context;
        }

        // GET: api/pagos?page=1&pageSize=8
        [HttpGet]
        public async Task<IActionResult> GetPagos([FromQuery] int page = 1, [FromQuery] int pageSize = 8)
        {
            var query = _context.Pagos
                .Include(p => p.Pedido)
                    .ThenInclude(ped => ped!.Mesa)
                .Include(p => p.Pedido)
                    .ThenInclude(ped => ped!.Mozo)
                .Include(p => p.UsuarioAnulacion)
                .AsQueryable();

            var totalItems = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.FechaPago)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.IdPago,
                    p.IdPedido,
                    MesaNumero = p.Pedido != null && p.Pedido.Mesa != null ? p.Pedido.Mesa.Numero : 0,
                    MozoNombre = p.Pedido != null && p.Pedido.Mozo != null ? $"{p.Pedido.Mozo.Nombre} {p.Pedido.Mozo.Apellido}" : "",
                    p.MedioPago,
                    p.Importe,
                    p.FechaPago,
                    p.Estado,
                    p.IdUsuarioAnulacion,
                    AdminAnulacionNombre = p.UsuarioAnulacion != null ? $"{p.UsuarioAnulacion.Nombre} {p.UsuarioAnulacion.Apellido}" : null,
                    p.FechaAnulacion
                })
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

        public record RegistrarPagoDto(int IdPedido, string MedioPago);

        // POST: api/pagos
        // Calcula el total sumando los Detalles del Pedido, registra el Pago, pasa Pedido a "Pagado" y Mesa a "Libre"
        [HttpPost]
        public async Task<IActionResult> RegistrarPago([FromBody] RegistrarPagoDto dto)
        {
            var mediosPermitidos = new[] { "Efectivo", "Tarjeta", "Transferencia" };
            if (!mediosPermitidos.Contains(dto.MedioPago))
                return BadRequest(new { mensaje = "Medio de pago inválido. Use Efectivo, Tarjeta o Transferencia." });

            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                .Include(p => p.Mesa)
                .FirstOrDefaultAsync(p => p.IdPedido == dto.IdPedido);

            if (pedido == null)
                return NotFound(new { mensaje = "Pedido no encontrado." });

            if (pedido.Estado != "Abierto")
                return BadRequest(new { mensaje = "El pedido no se encuentra Abierto." });

            if (!pedido.Detalles.Any())
                return BadRequest(new { mensaje = "No se puede cobrar un pedido sin productos cargados." });

            decimal total = pedido.Detalles.Sum(d => d.Cantidad * d.PrecioHistorico);
            var ahora = DateTime.Now;

            var nuevoPago = new Pago
            {
                IdPedido = pedido.IdPedido,
                MedioPago = dto.MedioPago,
                Importe = total,
                FechaPago = ahora,
                Estado = "Valido"
            };

            pedido.Estado = "Pagado";
            pedido.FechaCierre = ahora;

            if (pedido.Mesa != null)
            {
                pedido.Mesa.Estado = "Libre";
            }

            _context.Pagos.Add(nuevoPago);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Pago registrado con éxito. Mesa liberada.",
                pago = nuevoPago
            });
        }

        // POST: api/pagos/{id}/anular (Exclusivo Administrador)
        [HttpPost("{id}/anular")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AnularPago(int id)
        {
            var pago = await _context.Pagos
                .Include(p => p.Pedido)
                .FirstOrDefaultAsync(p => p.IdPago == id);

            if (pago == null)
                return NotFound(new { mensaje = "Pago no encontrado." });

            if (pago.Estado == "Anulado")
                return BadRequest(new { mensaje = "El pago ya se encuentra anulado." });

            int idAdmin = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1");
            var ahora = DateTime.Now;

            pago.Estado = "Anulado";
            pago.IdUsuarioAnulacion = idAdmin;
            pago.FechaAnulacion = ahora;

            if (pago.Pedido != null)
            {
                pago.Pedido.Estado = "Anulado";
                pago.Pedido.IdUsuarioAnulacion = idAdmin;
                pago.Pedido.FechaAnulacion = ahora;
            }

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = $"Pago #{id} y Pedido #{pago.IdPedido} anulados con auditoría." });
        }
    }
}
