using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using FeedHub_App.Views.News;

namespace FeedHub_App.ViewModels.News;

// 1. DEFINICIÓN DE LOS MODELOS (Fuera para que todos los vean)
public class NewsSource
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class SourceGroup : ObservableCollection<NewsSource>
{
    public string CategoryTitle { get; private set; }
    public SourceGroup(string title, IEnumerable<NewsSource> sources) : base(sources)
    {
        CategoryTitle = title;
    }
}

// 2. VIEWMODEL
public partial class CategoriesBySourceViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<SourceGroup> groupedSources;
    [ObservableProperty]
    private NewsSource selectedSource;
    public CategoriesBySourceViewModel()
    {
        var allSources = new List<NewsSource>
        {
            // --- PRENSA GENERAL ---
            new NewsSource { Id = "elpais", Name = "EL PAÍS", Description = "Noticias de España y el mundo", Domain = "elpais.com", Category = "PRENSA GENERAL" },
            new NewsSource { Id = "elmundo", Name = "EL MUNDO", Description = "Líder de información en español", Domain = "elmundo.es", Category = "PRENSA GENERAL" },
            new NewsSource { Id = "lavanguardia", Name = "LA VANGUARDIA", Description = "Noticias de última hora", Domain = "lavanguardia.com", Category = "PRENSA GENERAL" },
            new NewsSource { Id = "elperiodico", Name = "EL PERIÓDICO", Description = "Información y análisis", Domain = "elperiodico.com", Category = "PRENSA GENERAL" },
            new NewsSource { Id = "20minutos", Name = "20 MINUTOS", Description = "Actualidad rápida y directa", Domain = "20minutos.es", Category = "PRENSA GENERAL" },
            new NewsSource { Id = "elconfidencial", Name = "EL CONFIDENCIAL", Description = "Diario de lectores influyentes", Domain = "elconfidencial.com", Category = "PRENSA GENERAL" },
            new NewsSource { Id = "eldiarioes", Name = "ELDIARIO.ES", Description = "Periodismo independiente", Domain = "eldiario.es", Category = "PRENSA GENERAL" },
            new NewsSource { Id = "bbc", Name = "BBC MUNDO", Description = "Noticias internacionales", Domain = "bbc.com", Category = "PRENSA GENERAL" },

            // --- TECNOLOGÍA ---
            new NewsSource { Id = "xataka", Name = "XATAKA", Description = "Gadgets y tecnología", Domain = "xataka.com", Category = "TECNOLOGÍA" },
            new NewsSource { Id = "applesfera", Name = "APPLESFERA", Description = "Todo sobre el mundo Apple", Domain = "applesfera.com", Category = "TECNOLOGÍA" },
            new NewsSource { Id = "microsiervos", Name = "MICROSIERVOS", Description = "Ciencia, tecnología y curiosidades", Domain = "microsiervos.com", Category = "TECNOLOGÍA" },
            new NewsSource { Id = "hipertextual", Name = "HIPERTEXTUAL", Description = "Cultura digital y tecnología", Domain = "hipertextual.com", Category = "TECNOLOGÍA" },

            // --- ENTRETENIMIENTO ---
            new NewsSource { Id = "vidaextra", Name = "VIDAEXTRA", Description = "Mundo de los videojuegos", Domain = "vidaextra.com", Category = "ENTRETENIMIENTO" },
            new NewsSource { Id = "3djuegos", Name = "3DJUEGOS", Description = "Análisis y noticias de juegos", Domain = "3djuegos.com", Category = "ENTRETENIMIENTO" },
            new NewsSource { Id = "hobbyconsolas", Name = "HOBBYCONSOLAS", Description = "Videojuegos y entretenimiento", Domain = "hobbyconsolas.com", Category = "ENTRETENIMIENTO" },
            new NewsSource { Id = "ign", Name = "IGN ESPAÑA", Description = "Videojuegos, cine y series", Domain = "ign.com", Category = "ENTRETENIMIENTO" },

            // --- CIENCIA Y NATURALEZA ---
            new NewsSource { Id = "eltiempo", Name = "ELTIEMPO.ES", Description = "Previsión meteorológica", Domain = "eltiempo.es", Category = "CIENCIA" },
            new NewsSource { Id = "efeverde", Name = "EFE VERDE", Description = "Periodismo ambiental", Domain = "efeverde.com", Category = "CIENCIA" },
            new NewsSource { Id = "ecoticias", Name = "ECOTICIAS", Description = "Información ecológica", Domain = "ecoticias.com", Category = "CIENCIA" },
            new NewsSource { Id = "esa", Name = "ESA", Description = "Agencia Espacial Europea", Domain = "esa.int", Category = "CIENCIA" },
            new NewsSource { Id = "astroaficion", Name = "ASTROAFICIÓN", Description = "Pasión por la astronomía", Domain = "astroaficion.com", Category = "CIENCIA" },
            new NewsSource { Id = "fronteraespacial", Name = "FRONTERA ESPACIAL", Description = "Exploración del espacio", Domain = "fronteraespacial.com", Category = "CIENCIA" }
        };

        // Agrupamos y convertimos cada grupo explícitamente a nuestra clase SourceGroup
        var groups = allSources
            .GroupBy(s => s.Category)
            .Select(g => new SourceGroup(g.Key, g.ToList()));

        GroupedSources = new ObservableCollection<SourceGroup>(groups);
    }


    [RelayCommand]
    private async Task SelectSource(string sourceId)
    {
        if (!string.IsNullOrEmpty(sourceId))
            await Shell.Current.GoToAsync($"{nameof(NewsBySourcePage)}?source={sourceId}");
    }
}
