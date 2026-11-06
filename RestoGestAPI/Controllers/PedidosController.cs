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
    public class PedidosController : ControllerBase
    {
        private readonly DatabaseContext _context;

        public PedidosController(DatabaseContext context)
        {
            _context = context;
        }

        // GET: api/pedidos?page=1&pageSize=8&estado=&idMesa=
        // Cumple Requerimiento 5: Paginado por Servidor en historial de pedidos
        [HttpGet]
        public async Task<IActionResult> GetPedidos(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 8,
            [FromQuery] string? estado = null,
            [FromQuery] int? idMesa = null)
        {
            var query = _context.Pedidos
                .Include(p => p.Mesa)
                .Include(p => p.Mozo)
                .Include(p => p.UsuarioAnulacion)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(p => p.Estado == estado);

            if (idMesa.HasValue && idMesa.Value > 0)
                query = query.Where(p => p.IdMesa == idMesa.Value);

            var totalItems = await query.CountAsync();
            var pedidos = await query
                .OrderByDescending(p => p.FechaApertura)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = pedidos.Select(p => new
            {
                p.IdPedido,
                p.IdMesa,
                MesaNumero = p.Mesa != null ? p.Mesa.Numero : p.IdMesa,
                p.IdUsuarioMozo,
                MozoNombre = p.Mozo != null ? $"{p.Mozo.Nombre} {p.Mozo.Apellido}" : "",
                p.FechaApertura,
                p.FechaCierre,
                p.Estado,
                p.IdUsuarioAnulacion,
                AdminAnulacionNombre = p.UsuarioAnulacion != null ? $"{p.UsuarioAnulacion.Nombre} {p.UsuarioAnulacion.Apellido}" : null,
                p.FechaAnulacion,
                Total = p.Detalles.Sum(d => d.Cantidad * d.PrecioHistorico),
                Detalles = p.Detalles.Select(d => new
                {
                    d.IdDetalle,
                    d.IdProducto,
                    ProductoNombre = d.Producto != null ? d.Producto.Nombre : "",
                    d.Cantidad,
                    d.PrecioHistorico,
                    Subtotal = d.Cantidad * d.PrecioHistorico
                })
            });

            return Ok(new
            {
                items,
                totalItems,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            });
        }

        // GET: api/pedidos/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPedidoById(int id)
        {
            var p = await _context.Pedidos
                .Include(x => x.Mesa)
                .Include(x => x.Mozo)
                .Include(x => x.UsuarioAnulacion)
                .Include(x => x.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(x => x.IdPedido == id);

            if (p == null) return NotFound(new { mensaje = "Pedido no encontrado." });

            return Ok(new
            {
                p.IdPedido,
                p.IdMesa,
                MesaNumero = p.Mesa != null ? p.Mesa.Numero : p.IdMesa,
                p.IdUsuarioMozo,
                MozoNombre = p.Mozo != null ? $"{p.Mozo.Nombre} {p.Mozo.Apellido}" : "",
                p.FechaApertura,
                p.FechaCierre,
                p.Estado,
                Total = p.Detalles.Sum(d => d.Cantidad * d.PrecioHistorico),
                Detalles = p.Detalles.Select(d => new
                {
                    d.IdDetalle,
                    d.IdProducto,
                    ProductoNombre = d.Producto != null ? d.Producto.Nombre : "",
                    d.Cantidad,
                    d.PrecioHistorico,
                    Subtotal = d.Cantidad * d.PrecioHistorico
                })
            });
        }

        public record AbrirMesaDto(int IdMesa);

        // POST: api/pedidos/abrir
        // Valida que no se pueda abrir un pedido en una mesa que ya figura como "Ocupada"
        [HttpPost("abrir")]
        public async Task<IActionResult> AbrirMesa([FromBody] AbrirMesaDto dto)
        {
            var mesa = await _context.Mesas.FindAsync(dto.IdMesa);
            if (mesa == null || !mesa.Activo)
                return NotFound(new { mensaje = "La mesa seleccionada no existe." });

            if (mesa.Estado == "Ocupada")
                return BadRequest(new { mensaje = $"La Mesa #{mesa.Numero} ya se encuentra Ocupada." });

            var pedidoAbiertoExistente = await _context.Pedidos
                .AnyAsync(p => p.IdMesa == dto.IdMesa && p.Estado == "Abierto");
            if (pedidoAbiertoExistente)
                return BadRequest(new { mensaje = "La mesa ya posee una comanda abierta." });

            int idMozo = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1");

            mesa.Estado = "Ocupada";
            var nuevoPedido = new Pedido
            {
                IdMesa = mesa.IdMesa,
                IdUsuarioMozo = idMozo,
                FechaApertura = DateTime.Now,
                Estado = "Abierto"
            };

            _context.Pedidos.Add(nuevoPedido);
            await _context.SaveChangesAsync();

            return Ok(nuevoPedido);
        }

        public record AgregarDetalleDto(int IdProducto, int Cantidad);

        // POST: api/pedidos/{id}/detalles
        // Congela el precio_historico del producto al momento de pedir
        [HttpPost("{id}/detalles")]
        public async Task<IActionResult> AgregarItemAPedido(int id, [FromBody] AgregarDetalleDto dto)
        {
            if (dto.Cantidad <= 0)
                return BadRequest(new { mensaje = "La cantidad debe ser mayor a cero." });

            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.IdPedido == id);

            if (pedido == null)
                return NotFound(new { mensaje = "Pedido no encontrado." });

            if (pedido.Estado != "Abierto")
                return BadRequest(new { mensaje = "Solo se pueden agregar productos a un pedido en estado Abierto." });

            var producto = await _context.Productos.FindAsync(dto.IdProducto);
            if (producto == null || !producto.Activo || producto.Estado != "Disponible")
                return BadRequest(new { mensaje = "El producto no está disponible en stock." });

            var existente = pedido.Detalles.FirstOrDefault(d => d.IdProducto == dto.IdProducto && d.PrecioHistorico == producto.PrecioVigente);
            if (existente != null)
            {
                existente.Cantidad += dto.Cantidad;
            }
            else
            {
                var detalle = new DetallePedido
                {
                    IdPedido = pedido.IdPedido,
                    IdProducto = producto.IdProducto,
                    Cantidad = dto.Cantidad,
                    PrecioHistorico = producto.PrecioVigente
                };
                _context.DetallesPedido.Add(detalle);
            }

            await _context.SaveChangesAsync();
            return await GetPedidoById(id);
        }

        // DELETE: api/pedidos/{id}/detalles/{idDetalle}
        // Narrativa: "se pueden eliminar productos de un pedido mientras esté Abierto. Si el pedido ya fue pagado, un Empleado no puede modificarlo."
        [HttpDelete("{id}/detalles/{idDetalle}")]
        public async Task<IActionResult> QuitarItemDePedido(int id, int idDetalle)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
                return NotFound(new { mensaje = "Pedido no encontrado." });

            if (pedido.Estado != "Abierto")
                return BadRequest(new { mensaje = "No se pueden eliminar ítems de un pedido que ya fue Pagado o Anulado." });

            var detalle = await _context.DetallesPedido
                .FirstOrDefaultAsync(d => d.IdDetalle == idDetalle && d.IdPedido == id);

            if (detalle == null)
                return NotFound(new { mensaje = "Ítem de comanda no encontrado." });

            _context.DetallesPedido.Remove(detalle);
            await _context.SaveChangesAsync();

            return await GetPedidoById(id);
        }

        // POST: api/pedidos/{id}/anular
        // Requerimiento 2 & Narrativa: "Solo un Administrador puede anular un Pedido ya cerrado o abierto (baja lógica con auditoría)"
        [HttpPost("{id}/anular")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AnularPedido(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Mesa)
                .FirstOrDefaultAsync(p => p.IdPedido == id);

            if (pedido == null)
                return NotFound(new { mensaje = "Pedido no encontrado." });

            if (pedido.Estado == "Anulado")
                return BadRequest(new { mensaje = "El pedido ya se encuentra anulado." });

            int idAdmin = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1");
            var ahora = DateTime.Now;

            pedido.Estado = "Anulado";
            pedido.IdUsuarioAnulacion = idAdmin;
            pedido.FechaAnulacion = ahora;
            if (pedido.FechaCierre == null)
                pedido.FechaCierre = ahora;

            // Si la mesa estaba ocupada por este pedido, liberarla
            if (pedido.Mesa != null && pedido.Mesa.Estado == "Ocupada")
            {
                pedido.Mesa.Estado = "Libre";
            }

            // Si tenía pagos válidos asociados, anularlos lógicamente con auditoría
            var pagosAsociados = await _context.Pagos
                .Where(pg => pg.IdPedido == id && pg.Estado == "Valido")
                .ToListAsync();

            foreach (var pago in pagosAsociados)
            {
                pago.Estado = "Anulado";
                pago.IdUsuarioAnulacion = idAdmin;
                pago.FechaAnulacion = ahora;
            }

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = $"Pedido #{id} anulado lógicamente. Auditoría registrada por Administrador ID {idAdmin}." });
        }
    }
}
