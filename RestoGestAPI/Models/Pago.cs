namespace RestoGestAPI.Models
{
    public class Pago
    {
        public int IdPago { get; set; }
        public int IdPedido { get; set; }
        public string MedioPago { get; set; } // ENUM('Efectivo', 'Tarjeta', 'Transferencia')
        public decimal Importe { get; set; }
        public DateTime FechaPago { get; set; }
        public string Estado { get; set; } // ENUM('Valido', 'Anulado')
        public int? IdUsuarioAnulacion { get; set; }
        public DateTime? FechaAnulacion { get; set; }
    }
}