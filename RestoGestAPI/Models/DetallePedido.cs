using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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

        [JsonIgnore]
        [ForeignKey("IdPedido")]
        public Pedido? Pedido { get; set; }

        [Column("id_producto")]
        public int IdProducto { get; set; }

        [ForeignKey("IdProducto")]
        public Producto? Producto { get; set; }

        [Column("cantidad")]
        public int Cantidad { get; set; }

        [Column("precio_historico")]
        public decimal PrecioHistorico { get; set; }
    }
}
