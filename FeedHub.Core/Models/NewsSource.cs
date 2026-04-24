namespace FeedHub_Core.Models
{
    public class NewsSource
    {
        public string Name { get; set; } = string.Empty;       // Ej: "El País"
        public string Description { get; set; } = string.Empty; // Ej: "Periódico nacional"
        public string Domain { get; set; } = string.Empty;     // Ej: "elpais.com" (para el icono)
        public string CategoryName { get; set; } = string.Empty; // Ej: "PRENSA GENERAL"
        public string CommandParam { get; set; } = string.Empty; // Ej: "elpais"
    }
}
