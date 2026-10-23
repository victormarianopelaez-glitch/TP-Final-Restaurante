namespace RestoGestAPI.Models
{
    public class Usuario
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Rol { get; set; } // ENUM('Administrador', 'Mozo')
        public string AvatarRuta { get; set; }
        public bool Activo { get; set; }
    }
}