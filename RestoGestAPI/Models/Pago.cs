using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestoGestAPI.Models
{
    [Table("PAGO")]
    public class Pago
    {
        [Key]
        [Column("id_pago")]
        public int IdPago { get; set; }

        [Column("id_pedido")]
        public int IdPedido { get; set; }

        [ForeignKey("IdPedido")]
        public Pedido? Pedido { get; set; }

        [Column("medio_pago")]
        public string MedioPago { get; set; } = string.Empty; // ENUM('Efectivo', 'Tarjeta', 'Transferencia')

        [Column("importe")]
        public decimal Importe { get; set; }

        [Column("fecha_pago")]
        public DateTime FechaPago { get; set; }

        [Column("estado")]
        public string Estado { get; set; } = "Valido"; // ENUM('Valido', 'Anulado')

        [Column("id_usuario_anulacion")]
        public int? IdUsuarioAnulacion { get; set; }

        [ForeignKey("IdUsuarioAnulacion")]
        public Usuario? UsuarioAnulacion { get; set; }

        [Column("fecha_anulacion")]
        public DateTime? FechaAnulacion { get; set; }
    }
}