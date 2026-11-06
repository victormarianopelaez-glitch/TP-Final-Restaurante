using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RestoGestAPI.Models
{
    [Table("USUARIO")]
    public class Usuario
    {
        [Key]
        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Required]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [Column("apellido")]
        public string Apellido { get; set; } = string.Empty;

        [Column("telefono")]
        public string? Telefono { get; set; }

        [Required]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [JsonIgnore]
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