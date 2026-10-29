using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestoGestAPI.Models
{
    [Table("MESA")]
    public class Mesa
    {
        [Key]
        [Column("id_mesa")]
        public int IdMesa { get; set; }

        [Column("numero")]
        public int Numero { get; set; }

        [Column("capacidad")]
        public int Capacidad { get; set; }

        [Column("estado")]
        public string Estado { get; set; } = "Libre"; // ENUM('Libre', 'Ocupada')

        [Column("activo")]
        public bool Activo { get; set; } = true;
    }
}
