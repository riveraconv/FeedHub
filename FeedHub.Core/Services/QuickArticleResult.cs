using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace FeedHub_Core.Services
{
    public class QuickArticleResult
    {
        public string Html { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public class QuickArticleService
    {
        public QuickArticleResult Extract(string rawHtml)
        {
            if (string.IsNullOrWhiteSpace(rawHtml))
                return EmptyResult();

            var doc = new HtmlDocument();
            doc.LoadHtml(rawHtml);

            // 🔹 Imagen principal (og:image)
            var imageNode = doc.DocumentNode
                .SelectSingleNode("//meta[@property='og:image']")
                ?.GetAttributeValue("content", null);

            // 🔹 Título
            var titleNode = doc.DocumentNode.SelectSingleNode("//h1") ??
                            doc.DocumentNode.SelectSingleNode("//title");
            var titleText = titleNode?.InnerText.Trim() ?? "Sin título";

            // 🔹 Texto del artículo
            var articleNode = GetCleanArticleNode(doc);

            if (articleNode == null) return EmptyResult();

            string textContent = articleNode != null
                ? CleanText(articleNode.InnerHtml)
                : "No se pudo extraer el contenido.";

            textContent = RemoveResidualGarbage(textContent);

            // 🔹 HTML ligero para WebView

string html = $@"
<html>
<head>
<meta charset='utf-8'>
<style>
:root {{
    color-scheme: light dark;
}}

html, body {{
    font-family: 'Segoe UI', 'Roboto', 'Helvetica Neue', sans-serif;
    line-height: 1.75;
    color: #f8f8f8;
    background-color: #1e2430;
    padding: 1.5em;
    max-width: 800px;
    margin: auto;
    border-radius: 16px;
    box-shadow: 0 0 20px rgba(0,0,0,0.25);
}}

/* 🔹 Soporte automático tema claro */
@media (prefers-color-scheme: light) {{
    body {{
        background-color: #1e2430;
        color: #f8f8f8;
        box-shadow: 0 0 15px rgba(0,0,0,0.1);
    }}
}}

h1 {{
    font-size: 1.9em;
    margin-bottom: 0.8em;
    font-weight: 700;
    color: #ffffff;
    text-align: center;
}}

img {{
    max-width: 100%;
    height: auto;
    margin: 1em 0;
    border-radius: 10px;
    display: block;
}}

p {{
    margin: 1em 0;
    text-align: justify;
    font-size: 1.1em;
    font-weight: 500;
}}

a {{
    color: #4a9eff;
    text-decoration: none;
    font-weight: 600;
}}

a:hover {{
    text-decoration: underline;
}}

body::selection {{
    background: #4a9eff;
    color: #fff;
}}
</style>
</head>
<body>
<h1>{titleText}</h1>
{(string.IsNullOrEmpty(imageNode) ? "" : $"<img src='{imageNode}' />")}
{textContent}
</body>
</html>";


            return new QuickArticleResult
            {
                Html = html,
                Title = titleText,
                ImageUrl = imageNode
            };
        }

        private string CleanText(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;

            // Quitar scripts, estilos, comentarios
            html = Regex.Replace(html, @"<script.*?>.*?</script>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<style.*?>.*?</style>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<!--.*?-->", "", RegexOptions.Singleline);

            // Eliminar secciones típicas de basura
            string[] spamPatterns = {
                "síguenos", "suscríbete", "publicidad", "te puede interesar",
                "leer más", "comparte", "newsletter", "tweet", "facebook", "compartir",
                "suscripción", "regístrate", "pago", "premium",
                "añadir usuario", "continuar leyendo", "leer más en", "solo para suscriptores",
                "inicia sesión", "tu cuenta", "tu suscripción", "compartir en",
                "newsletter", "síguenos en", "comentarios",
                "cambiar tu contraseña", "modo premium", "accede aquí"
            };

            foreach (var pattern in spamPatterns)
                html = Regex.Replace(html, $@"<[^>]*>{pattern}.*?</[^>]*>", "", RegexOptions.IgnoreCase);

            // Quitar etiquetas innecesarias y normalizar
            html = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"</p>", "\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<.*?>", "");
            html = Regex.Replace(html, @"\s{2,}", " ").Trim();

            // Reconstruir en párrafos
            var paragraphs = html.Split('\n')
                                 .Select(p => p.Trim())
                                 .Where(p => !string.IsNullOrEmpty(p))
                                 .Select(p => $"<p>{p}</p>");

            return string.Join("\n", paragraphs);
        }
        private string RemoveResidualGarbage(string html)
        {
            string[] residuals =
            {
        "©", "Copyright", "Reuters", "Europa Press", "EFE",
        "Redacción", "Actualizado", "Publicado", "Fuente:"
    };

            foreach (var r in residuals)
                html = Regex.Replace(html, @$"<p>.*{r}.*</p>", "", RegexOptions.IgnoreCase);

            return html;
        }


        private HtmlNode? GetLargestTextNode(HtmlNode root)
        {
            if (root == null) return null;
            HtmlNode? largest = null;
            int maxLength = 0;

            foreach (var node in root.Descendants("div").Concat(root.Descendants("section")).Concat(root.Descendants("article")))
            {
                var text = node.InnerText?.Trim() ?? "";
                if (text.Length > maxLength)
                {
                    maxLength = text.Length;
                    largest = node;
                }
            }

            return largest ?? root;
        }

        private QuickArticleResult EmptyResult()
        {
            return new QuickArticleResult
            {
                Html = "<html><body><p>No se pudo cargar el contenido.</p></body></html>",
                Title = "Sin título",
                ImageUrl = string.Empty
            };
        }

        private HtmlNode? GetCleanArticleNode(HtmlDocument doc)
        {
            if (doc.DocumentNode == null)
                return null;

            var articleNode = doc.DocumentNode.SelectSingleNode("//article") ??
                              doc.DocumentNode.SelectSingleNode("//div[contains(@class,'content')]") ??
                              doc.DocumentNode.SelectSingleNode("//div[@itemprop='articleBody']") ??
                              GetLargestTextNode(doc.DocumentNode);

            if (articleNode == null)
                return null;

            // Eliminar nodos de ruido
            var unwantedSelectors = new[]
            {
        "//aside", "//footer", "//nav",
        "//*[contains(@class,'share')]",
        "//*[contains(@class,'social')]",
        "//*[contains(@class,'related')]",
        "//*[contains(@class,'recommend')]",
        "//*[contains(@class,'comments')]",
        "//*[contains(@class,'newsletter')]",
        "//*[contains(@class,'advert')]",     // anuncios
        "//*[contains(@id,'comments')]",
        "//*[contains(@id,'subscription')]",  // 🔥 paywalls
        "//*[contains(@class,'subscription')]",
        "//*[contains(@class,'paywall')]",    // 🔥 bloqueos de lectura
        "//*[contains(@class,'modal')]",      // popups
        "//*[contains(@class,'overlay')]",    // capas grises
        "//*[contains(@class,'access')]",     // mensajes de acceso restringido
        "//*[contains(@id,'paywall')]",       // id específicos
        "//*[contains(@id,'overlay')]"        // id overlays
    };

            foreach (var selector in unwantedSelectors)
            {
                var nodes = articleNode.SelectNodes(selector)?.ToList();
                if (nodes == null) continue;

                foreach (var node in nodes)
                    node.Remove();
            }

            return articleNode;
        }

    }
}

