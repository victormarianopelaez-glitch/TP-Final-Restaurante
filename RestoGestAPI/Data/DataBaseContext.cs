using MySql.Data.MySqlClient;
using Microsoft.EntityFrameworkCore;
using RestoGestAPI.Models;

namespace RestoGestAPI.Data
{
    public class DatabaseContext : DbContext
    {

        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {

        }

        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Mesa> Mesas { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<DetallePedido> DetallesPedido { get; set; }

    }
}