using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestoGestAPI.Models
{
    [Table("USUARIO")]
    public class Usuario
    {
        [Key]
        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("apellido")]
        public string Apellido { get; set; } = string.Empty;

        [Column("telefono")]
        public string? Telefono { get; set; }

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("password")]
        public string Password { get; set; } = string.Empty;

        [Column("rol")]
        public string Rol { get; set; } = string.Empty; // ENUM('Administrador', 'Mozo')

        [Column("avatar_ruta")]
        public string? AvatarRuta { get; set; }

        [Column("activo")]
        public bool Activo { get; set; } = true;
    }
}