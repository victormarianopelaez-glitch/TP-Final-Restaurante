namespace RestoGestAPI.Models
{
    public class Pedido
    {
        public int IdPedido { get; set; }
        public int IdMesa { get; set; }
        public int IdUsuarioMozo { get; set; }
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }
        public string Estado { get; set; } // ENUM('Abierto', 'Pagado', 'Anulado')
        public int? IdUsuarioAnulacion { get; set; }
        public DateTime? FechaAnulacion { get; set; }
    }
}
