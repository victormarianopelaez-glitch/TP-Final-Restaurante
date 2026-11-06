using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestoGestAPI.Data;

namespace RestoGestAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InformesController : ControllerBase
    {
        private readonly DatabaseContext _context;

        public InformesController(DatabaseContext context)
        {
            _context = context;
        }

        // Informe 1: Listar todos los productos agrupados por Categoría (filtrando por disponibilidad)
        [HttpGet("productos-por-categoria")]
        public async Task<IActionResult> ProductosPorCategoria([FromQuery] string? estado = null)
        {
            var categorias = await _context.Categorias
                .Where(c => c.Activo)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            var prodQuery = _context.Productos.Where(p => p.Activo).AsQueryable();
            if (!string.IsNullOrWhiteSpace(estado))
            {
                prodQuery = prodQuery.Where(p => p.Estado == estado);
            }

            var productos = await prodQuery.OrderBy(p => p.Nombre).ToListAsync();

            var resultado = categorias.Select(c => new
            {
                c.IdCategoria,
                Categoria = c.Nombre,
                Descripcion = c.Descripcion,
                Productos = productos.Where(p => p.IdCategoria == c.IdCategoria).Select(p => new
                {
                    p.IdProducto,
                    p.Nombre,
                    p.Descripcion,
                    p.PrecioVigente,
                    p.Estado,
                    p.ImagenRuta
                }).ToList()
            }).Where(g => g.Productos.Any()).ToList();

            return Ok(resultado);
        }

        // Informe 2: Listar los productos más vendidos en los últimos X días (permitir elegir plazo)
        [HttpGet("mas-vendidos")]
        public async Task<IActionResult> ProductosMasVendidos([FromQuery] int dias = 30)
        {
            if (dias <= 0) dias = 30;
            var fechaDesde = DateTime.Now.AddDays(-dias);

            var detalles = await _context.DetallesPedido
                .Include(d => d.Pedido)
                .Include(d => d.Producto)
                    .ThenInclude(p => p!.Categoria)
                .Where(d => d.Pedido != null
                            && d.Pedido.Estado == "Pagado"
                            && d.Pedido.FechaApertura >= fechaDesde)
                .ToListAsync();

            var ranking = detalles
                .GroupBy(d => new
                {
                    d.IdProducto,
                    Nombre = d.Producto != null ? d.Producto.Nombre : $"Producto #{d.IdProducto}",
                    Categoria = d.Producto != null && d.Producto.Categoria != null ? d.Producto.Categoria.Nombre : "Sin categoría"
                })
                .Select(g => new
                {
                    g.Key.IdProducto,
                    g.Key.Nombre,
                    g.Key.Categoria,
                    CantidadVendida = g.Sum(x => x.Cantidad),
                    TotalRecaudado = g.Sum(x => x.Cantidad * x.PrecioHistorico)
                })
                .OrderByDescending(x => x.CantidadVendida)
                .ThenByDescending(x => x.TotalRecaudado)
                .ToList();

            return Ok(new { dias, fechaDesde, items = ranking });
        }

        // Informe 3: Listar las mesas ocupadas actualmente (Pedidos en estado Abierto),
        // mostrando hace cuánto tiempo están ocupadas y el subtotal consumido hasta el momento
        [HttpGet("mesas-ocupadas")]
        public async Task<IActionResult> MesasOcupadasActualmente()
        {
            var abiertos = await _context.Pedidos
                .Include(p => p.Mesa)
                .Include(p => p.Mozo)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .Where(p => p.Estado == "Abierto")
                .OrderBy(p => p.FechaApertura)
                .ToListAsync();

            var ahora = DateTime.Now;
            var resultado = abiertos.Select(p =>
            {
                var duracion = ahora - p.FechaApertura;
                return new
                {
                    p.IdPedido,
                    p.IdMesa,
                    MesaNumero = p.Mesa != null ? p.Mesa.Numero : p.IdMesa,
                    Capacidad = p.Mesa != null ? p.Mesa.Capacidad : 0,
                    Mozo = p.Mozo != null ? $"{p.Mozo.Nombre} {p.Mozo.Apellido}" : "",
                    p.FechaApertura,
                    MinutosOcupada = (int)Math.Max(0, duracion.TotalMinutes),
                    TiempoFormateado = $"{(int)duracion.TotalHours}h {duracion.Minutes}m",
                    SubtotalConsumido = p.Detalles.Sum(d => d.Cantidad * d.PrecioHistorico),
                    CantidadItems = p.Detalles.Sum(d => d.Cantidad)
                };
            });

            return Ok(resultado);
        }

        // Informe 4: Listar la recaudación total del restaurante en un rango de fechas determinado
        // (filtrando solo Pedidos pagados válidos, excluyendo anulados)
        [HttpGet("recaudacion")]
        public async Task<IActionResult> RecaudacionPorFechas([FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null)
        {
            var fechaInicio = (desde ?? DateTime.Today.AddDays(-30)).Date;
            var fechaFin = (hasta ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

            var pagosValidos = await _context.Pagos
                .Include(p => p.Pedido)
                    .ThenInclude(ped => ped!.Mozo)
                .Include(p => p.Pedido)
                    .ThenInclude(ped => ped!.Mesa)
                .Where(p => p.Estado == "Valido"
                            && p.Pedido != null
                            && p.Pedido.Estado == "Pagado"
                            && p.FechaPago >= fechaInicio
                            && p.FechaPago <= fechaFin)
                .OrderByDescending(p => p.FechaPago)
                .ToListAsync();

            var porMedioPago = pagosValidos
                .GroupBy(p => p.MedioPago)
                .Select(g => new
                {
                    MedioPago = g.Key,
                    CantidadPagos = g.Count(),
                    Subtotal = g.Sum(x => x.Importe)
                })
                .ToList();

            var porMozo = pagosValidos
                .GroupBy(p => p.Pedido?.Mozo != null ? $"{p.Pedido.Mozo.Nombre} {p.Pedido.Mozo.Apellido}" : "Sin asignar")
                .Select(g => new
                {
                    Mozo = g.Key,
                    PedidosAtendidos = g.Count(),
                    TotalFacturado = g.Sum(x => x.Importe)
                })
                .ToList();

            return Ok(new
            {
                desde = fechaInicio,
                hasta = fechaFin,
                recaudacionTotal = pagosValidos.Sum(p => p.Importe),
                cantidadTicketsPagados = pagosValidos.Count,
                porMedioPago,
                porMozo,
                detallePagos = pagosValidos.Select(p => new
                {
                    p.IdPago,
                    p.IdPedido,
                    MesaNumero = p.Pedido?.Mesa?.Numero ?? 0,
                    Mozo = p.Pedido?.Mozo != null ? $"{p.Pedido.Mozo.Nombre} {p.Pedido.Mozo.Apellido}" : "",
                    p.MedioPago,
                    p.Importe,
                    p.FechaPago
                })
            });
        }

        // Informe 5: Listado de auditoría (solo para administrador):
        // Listar todos los pedidos o pagos anulados, indicando fecha, monto y qué usuario administrador realizó la anulación
        [HttpGet("auditoria-anulaciones")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AuditoriaAnulaciones()
        {
            var pedidosAnulados = await _context.Pedidos
                .Include(p => p.Mesa)
                .Include(p => p.Mozo)
                .Include(p => p.UsuarioAnulacion)
                .Include(p => p.Detalles)
                .Where(p => p.Estado == "Anulado")
                .OrderByDescending(p => p.FechaAnulacion ?? p.FechaApertura)
                .ToListAsync();

            var pagosAnulados = await _context.Pagos
                .Include(pg => pg.UsuarioAnulacion)
                .Where(pg => pg.Estado == "Anulado")
                .ToListAsync();

            var registros = pedidosAnulados.Select(p =>
            {
                var pago = pagosAnulados.FirstOrDefault(pg => pg.IdPedido == p.IdPedido);
                decimal monto = pago != null ? pago.Importe : p.Detalles.Sum(d => d.Cantidad * d.PrecioHistorico);
                return new
                {
                    p.IdPedido,
                    IdPago = pago?.IdPago,
                    MedioPago = pago?.MedioPago ?? "Sin cobro (Comanda anulada)",
                    MesaNumero = p.Mesa != null ? p.Mesa.Numero : p.IdMesa,
                    MozoOriginal = p.Mozo != null ? $"{p.Mozo.Nombre} {p.Mozo.Apellido}" : "",
                    MontoAnulado = monto,
                    FechaApertura = p.FechaApertura,
                    FechaAnulacion = p.FechaAnulacion ?? pago?.FechaAnulacion,
                    IdAdminAnulacion = p.IdUsuarioAnulacion ?? pago?.IdUsuarioAnulacion,
                    AdminResponsable = p.UsuarioAnulacion != null
                        ? $"{p.UsuarioAnulacion.Nombre} {p.UsuarioAnulacion.Apellido} ({p.UsuarioAnulacion.Email})"
                        : (pago?.UsuarioAnulacion != null ? $"{pago.UsuarioAnulacion.Nombre} {pago.UsuarioAnulacion.Apellido}" : "Administrador")
                };
            });

            return Ok(registros);
        }
    }
}
