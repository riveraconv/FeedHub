namespace FeedHub_Core.Models;

public class FilterItem
{
    // El nombre que verá el usuario (ej: "EL PAÍS" o "TECNOLOGÍA")
    public string Title { get; set; } = string.Empty;
    
    // El ID interno para filtrar (ej: "elpais" o "tecnologia")
    public string Code { get; set; } = string.Empty;
    
    // Lo que vincularemos al Switch en el XAML
    public bool IsActive { get; set; }

}
