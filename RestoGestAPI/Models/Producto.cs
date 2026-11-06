using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestoGestAPI.Models
{
    [Table("PRODUCTO")]
    public class Producto
    {
        [Key]
        [Column("id_producto")]
        public int IdProducto { get; set; }

        [Column("id_categoria")]
        public int IdCategoria { get; set; }

        [ForeignKey("IdCategoria")]
        public Categoria? Categoria { get; set; }
        
        [Required]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("precio_vigente")]
        public decimal PrecioVigente { get; set; }

        [Column("imagen_ruta")]
        public string? ImagenRuta { get; set; }

        [Column("estado")]
        public string Estado { get; set; } = "Disponible"; // ENUM('Disponible', 'Sin stock')

        [Column("activo")]
        public bool Activo { get; set; } = true;
    }
}