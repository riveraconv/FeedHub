using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using FeedHub_App.Views.News;


namespace FeedHub_App.ViewModels.News;

public partial class CategoriesBySourceViewModel : ObservableObject
{
    public ObservableCollection<NewsSource> Sources { get; set; }

    public CategoriesBySourceViewModel()
    {
        Sources = new ObservableCollection<NewsSource>
        {
            new NewsSource { Id = "elpais", Name = "EL PAÍS" },
            new NewsSource { Id = "elmundo", Name = "EL MUNDO" },
            new NewsSource { Id = "lavanguardia", Name = "LA VANGUARDIA" },
            new NewsSource { Id = "elperiodico", Name = "EL PERIÓDICO" },
            new NewsSource { Id = "20minutos", Name = "20 MINUTOS" },
            new NewsSource { Id = "elconfidencial", Name = "EL CONFIDENCIAL" },
            new NewsSource { Id = "eldiarioes", Name = "ELDIARIO.ES" },
            new NewsSource { Id = "bbc", Name = "BBC MUNDO" },
            new NewsSource { Id = "xataka", Name = "XATAKA" },
            new NewsSource { Id = "applesfera", Name = "APPLESFERA" },
            new NewsSource { Id = "microsiervos", Name = "MICROSIERVOS" },
            new NewsSource { Id = "hypertextual", Name = "HYPERTEXTUAL" },
            new NewsSource { Id = "vidaextra", Name = "VIDAEXTRA" },
            new NewsSource { Id = "3djuegos", Name = "3DJUEGOS" },
            new NewsSource { Id = "hobbyconsolas", Name = "HOBBYCONSOLAS" },
            new NewsSource { Id = "ign", Name = "IGN ESPAÑA" },
            new NewsSource { Id = "eltiempo", Name = "ELTIEMPO.ES" },
            new NewsSource { Id = "efeverde", Name = "EFE VERDE" },
            new NewsSource { Id = "econoticias", Name = "ECONOTICIAS" },
            new NewsSource { Id = "esa", Name = "ESA" },
            new NewsSource { Id = "astroaficion", Name = "ASTROAFICIÓN" },
            new NewsSource { Id = "fronteraespacial", Name = "FRONTERA ESPACIAL" }
        };
    }

    [RelayCommand]
    private async Task SelectSource(string sourceId)
    {
        if (string.IsNullOrEmpty(sourceId)) return;

        await Shell.Current.GoToAsync($"{nameof(NewsBySourcePage)}?source={sourceId}");
    }

    public class NewsSource
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
