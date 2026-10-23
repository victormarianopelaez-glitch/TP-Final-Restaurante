namespace RestoGestAPI.Models
{
    public class Producto
    {
        public int IdProducto { get; set; }
        public int IdCategoria { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal PrecioVigente { get; set; } 
        public string ImagenRuta { get; set; }
        public string Estado { get; set; } // ENUM('Disponible', 'Sin stock')
        public bool Activo { get; set; }
    }
}