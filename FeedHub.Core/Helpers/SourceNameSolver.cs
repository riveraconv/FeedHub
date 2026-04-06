using System;

namespace FeedHub_Core.Helpers;

public static class SourceNameSolver
{
    private static readonly Dictionary<string, string> _domainMap = new()
    {
        { "elpais.com", "El País" },
        { "elmundo.es", "El Mundo" },
        { "lavanguardia.com", "La Vanguardia" },
        { "elperiodico.com", "El Periódico" },
        { "20minutos.es", "20 Minutos" },
        { "elconfidencial.com", "El Confidencial" },
        { "eldiario.es", "elDiario.es" },
        { "bbc.co.uk", "BBC Mundo" },
        { "xataka.com", "Xataka" },
        { "applesfera.com", "Applesfera" },
        { "microsiervos.com", "Microsiervos" },
        { "hipertextual.com", "Hipertextual" },
        { "vidaextra.com", "VidaExtra" },
        { "espinof.com", "Espinof" },
        { "eurogamer.es", "Eurogamer" },
        { "3djuegos.com", "3DJuegos" },
        { "hobbyconsolas.com", "HobbyConsolas" },
        { "ign.com", "IGN España" },
        { "eltiempo.es", "ElTiempo.es" },
        { "efeverde.com", "EFE Verde" },
        { "ecoticias.com", "Ecoticias" },
        { "esa.int", "ESA" },
        { "astroaficion.com", "AstroAfición" },
        { "fronteraespacial.com", "Frontera Espacial" },
    };

    public static string Resolve(string? url)
    {
        if (string.IsNullOrEmpty(url)) return "Desconocida";

        try
        {
            var host = new Uri(url).Host.Replace("www.", "");
            foreach (var kvp in _domainMap)
                if (host.Contains(kvp.Key))
                    return kvp.Value;

            // Si no está en el mapa, devuelve el dominio limpio
            return host.Split('.')[0].ToUpperInvariant();
        }
        catch
        {
            return "Desconocida";
        }
    }
}

