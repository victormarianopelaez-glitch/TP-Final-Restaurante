using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestoGestAPI.Models
{
    [Table("PEDIDO")]
    public class Pedido
    {
        [Key]
        [Column("id_pedido")]
        public int IdPedido { get; set; }

        [Column("id_mesa")]
        public int IdMesa { get; set; }

        [Column("id_usuario_mozo")]
        public int IdUsuarioMozo { get; set; }

        [Column("fecha_apertura")]
        public DateTime FechaApertura { get; set; }

        [Column("fecha_cierre")]
        public DateTime? FechaCierre { get; set; }

        [Column("estado")]
        public string Estado { get; set; } = "Abierto"; // ENUM('Abierto', 'Pagado', 'Anulado')

        [Column("id_usuario_anulacion")]
        public int? IdUsuarioAnulacion { get; set; }

        [Column("fecha_anulacion")]
        public DateTime? FechaAnulacion { get; set; }
    }
}
