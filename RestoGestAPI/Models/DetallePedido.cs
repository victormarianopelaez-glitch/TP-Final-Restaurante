using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestoGestAPI.Models
{
    [Table("DETALLE_PEDIDO")]
    public class DetallePedido
    {
        [Key]
        [Column("id_detalle")]
        public int IdDetalle { get; set; }

        [Column("id_pedido")]
        public int IdPedido { get; set; }

        [Column("id_producto")]
        public int IdProducto { get; set; }

        [Column("cantidad")]
        public int Cantidad { get; set; }

        [Column("precio_historico")]
        public decimal PrecioHistorico { get; set; }
    }
}
