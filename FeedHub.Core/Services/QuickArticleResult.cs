using HtmlAgilityPack;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Threading.Tasks;

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
        public async Task<QuickArticleResult> Extract(string rawHtml)
        {
            if (string.IsNullOrWhiteSpace(rawHtml))
                return EmptyResult();

            var doc = new HtmlDocument();
            doc.LoadHtml(rawHtml);

            // 🔹 Título
            var titleNode = doc.DocumentNode.SelectSingleNode("//h1") ??
                            doc.DocumentNode.SelectSingleNode("//title");

            var titleText = titleNode?.InnerText.Trim() ?? "Sin título";

            // 🔹 Texto del artículo
            var articleNode = GetCleanArticleNode(doc);
            if (articleNode == null)
                return EmptyResult();

            string textContent = CleanText(articleNode.InnerHtml);
            textContent = RemoveResidualGarbage(textContent);

            // 🔹 Imagen principal (robusta)
            var imageUrl = ExtractBestImage(doc, articleNode) ?? string.Empty;

            return new QuickArticleResult
            {
                Html = textContent,
                Title = titleText,
                ImageUrl = imageUrl
            };
        }


        private string? ExtractBestImage(HtmlDocument doc, HtmlNode? articleNode = null)
        {
            if (doc == null) return null;

            var baseUrl =
                doc.DocumentNode
                   .SelectSingleNode("//meta[@property='og:url']")
                   ?.GetAttributeValue("content", null);

            // 1️⃣ og:image
            var ogImage =
                doc.DocumentNode
                   .SelectSingleNode("//meta[@property='og:image']")
                   ?.GetAttributeValue("content", null);

            var normalized = NormalizeImageUrl(ogImage, baseUrl);
            if (IsValidImageUrl(normalized))
                return normalized;

            // 2️⃣ twitter:image
            var twitterImage =
                doc.DocumentNode
                   .SelectSingleNode("//meta[@name='twitter:image']")
                   ?.GetAttributeValue("content", null);

            normalized = NormalizeImageUrl(twitterImage, baseUrl);
            if (IsValidImageUrl(normalized))
                return normalized;

            // 3️⃣ Primera imagen real del artículo
            var imgSrc = articleNode
    .Descendants("img")
    .Select(img =>
        img.GetAttributeValue("data-src", null) ??
        img.GetAttributeValue("data-original", null) ??
        ExtractFromSrcSet(img.GetAttributeValue("srcset", null)) ??
        img.GetAttributeValue("src", null))
    .FirstOrDefault(src =>
        !string.IsNullOrWhiteSpace(src) &&
        !src.Contains("sprite") &&
        !src.Contains("logo") &&
        !src.StartsWith("data:"));

            normalized = NormalizeImageUrl(imgSrc, baseUrl);
            if (IsValidImageUrl(normalized))
                return normalized;

            return null;
        }
        private string? ExtractFromSrcSet(string? srcset)
        {
            if (string.IsNullOrWhiteSpace(srcset))
                return null;

            // Formato: url1 300w, url2 600w
            var candidates = srcset.Split(',')
                .Select(p => p.Trim().Split(' '))
                .Where(p => p.Length > 0)
                .Select(p => p[0])
                .ToList();

            return candidates.LastOrDefault();
        }
        private bool IsValidImageUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || url.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                || url.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || url.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
        }

        private string? NormalizeImageUrl(string? rawUrl, string? baseUrl)
        {
            if (string.IsNullOrWhiteSpace(rawUrl))
                return null;

            if (Uri.TryCreate(rawUrl, UriKind.Absolute, out var absolute))
                return absolute.ToString();

            if (!string.IsNullOrWhiteSpace(baseUrl) &&
                Uri.TryCreate(new Uri(baseUrl), rawUrl, out var combined))
                return combined.ToString();

            return null;
        }

        private string CleanText(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            // 1️⃣ Quitar scripts, estilos y comentarios
            html = Regex.Replace(html, @"<script.*?>.*?</script>|<style.*?>.*?</style>|", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            // 2️⃣ IMPORTANTE: Antes de borrar etiquetas, reemplazamos bloques que separan texto por un espacio
            // Esto evita que "Barcelona" y "17" se peguen si estaban en celdas o divs distintos.
            html = Regex.Replace(html, @"(?i)</?(address|blockquote|center|div|dt|dd|fieldset|form|h1|h2|h3|h4|h5|h6|li|p|pre|tr|td|section|article|aside|header|footer)>", " ");

            // 3️⃣ Limpieza selectiva: Borramos todo EXCEPTO negritas, cursivas y enlaces
            html = Regex.Replace(html, @"<(?!/?(b|strong|i|em|a)\b)[^>]*>", "");

            // 4️⃣ Decodificar entidades HTML (ej: &nbsp; o &aacute;)
            html = WebUtility.HtmlDecode(html);

            // 5️⃣ SOLUCIÓN A LAS FECHAS: Limpiar espacios múltiples, tabuladores y saltos de línea internos
            // Reemplaza cualquier secuencia de espacios/tabs/saltos por UN solo espacio
            html = Regex.Replace(html, @"\s+", " ");

            // 6️⃣ Filtrar palabras de spam sobre cada línea
            string[] spamPatterns = {
        "síguenos", "suscríbete", "publicidad", "te puede interesar",
        "leer más", "comparte", "newsletter", "tweet", "facebook", "compartir",
        "suscripción", "regístrate", "pago", "premium",
        "añadir usuario", "continuar leyendo", "leer más en", "solo para suscriptores",
        "inicia sesión", "tu cuenta", "tu suscripción", "compartir en",
        "comentarios", "cambiar tu contraseña", "modo premium", "accede aquí", "Por qué estás viendo esto?", "Por qué confiar en el Periódico",
        "Cada uno accederá con su propia cuenta de email, lo que os permitirá personalizar vuestra experiencia en el PAÍS."
    };
            var lines = html.Split(new[] { ". " }, StringSplitOptions.RemoveEmptyEntries);

            var paragraphs = lines
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p)
                         && p.Length > 5
                         && !spamPatterns.Any(spam => p.Contains(spam, StringComparison.OrdinalIgnoreCase)))
                .Select(p => p.EndsWith(".") ? $"<p>{p}</p>" : $"<p>{p}.</p>");

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
            if (doc.DocumentNode == null) return null;

            var unwantedSelectors = new[]
            {
                "//aside", "//footer", "//nav", "//header",
                "//*[contains(@class,'ed-sections')]",  // El menú azul de la imagen 1
                "//*[contains(@class,'site-map')]",
                "//*[contains(@class,'related')]",     // Bloques de "Te puede interesar"
                "//*[contains(@class,'featured-related')]", // Noticias sugeridas (Imagen 2 y 3)
                "//*[contains(@class,'ad-')]",
                "//*[contains(@class,'social')]",
                "//*[contains(@class,'newsletter')]",
                "//*[contains(@class,'magazine-promo')]"
            };

            foreach (var selector in unwantedSelectors)
            {
                var nodes = doc.DocumentNode.SelectNodes(selector)?.ToList();
                if (nodes != null) foreach (var n in nodes) n.Remove();
            }

            // 1️⃣ Intentar selectores semánticos muy específicos primero
            // elDiario.es usa "article-text" para el cuerpo real
            var articleNode =
                doc.DocumentNode.SelectSingleNode("//div[@itemprop='articleBody']") ??
                doc.DocumentNode.SelectSingleNode("//div[contains(@class,'article-text')]") ??
                doc.DocumentNode.SelectSingleNode("//article") ??
                doc.DocumentNode.SelectSingleNode("//main") ??

                GetBestContentNode(doc.DocumentNode);
            // 2️⃣ Si no hay suerte, buscamos el nodo con más párrafos reales
            GetBestContentNode(doc.DocumentNode);

            if (articleNode == null) return null;

            

            // 4️⃣ LIMPIEZA FINAL: Si después de todo, el nodo resultante sigue siendo 
            // mayoritariamente enlaces, buscamos dentro de él solo los párrafos.
            if (CalculateLinkDensity(articleNode) > 0.5)
            {
                // Si falló y pilló un menú, intentamos buscar solo los párrafos <p> 
                // que tengan una longitud decente dentro del nodo.
                var paragraphs = articleNode.SelectNodes(".//p[string-length(text()) > 40]");
                if (paragraphs != null)
                {
                    var cleanNode = HtmlNode.CreateNode("<div></div>");
                    foreach (var p in paragraphs) cleanNode.AppendChild(p.Clone());
                    return cleanNode;
                }
            }

            return articleNode;
        }
        private HtmlNode? GetBestContentNode(HtmlNode root)
        {
            // Buscamos el nodo que tenga la mejor relación entre párrafos y longitud
            return root.Descendants()
                .Where(n => (n.Name == "div" || n.Name == "section") && n.HasChildNodes)
                .OrderByDescending(n => {
                    var ps = n.SelectNodes("./p");
                    if (ps == null) return 0;

                    // PUNTUACIÓN:
                    // +100 por cada párrafo largo (>50 caracteres)
                    // -50 si el nodo tiene demasiados enlaces (densidad de links)
                    int score = ps.Count(p => p.InnerText.Trim().Length > 50) * 100;

                    if (CalculateLinkDensity(n) > 0.5) score -= 1000; // Penalizar menús

                    return score;
                })
                .FirstOrDefault();
        }

        private double CalculateLinkDensity(HtmlNode node)
        {
            var text = node.InnerText.Trim();
            if (string.IsNullOrWhiteSpace(text)) return 0;

            var linkText = string.Join("", node.SelectNodes(".//a")?.Select(a => a.InnerText) ?? Enumerable.Empty<string>());
            return (double)linkText.Length / text.Length;
        }
        public string DecodeHtml(byte[] bytes, string? charset)
        {
            // 1️⃣ Detect or fallback to UTF-8
            Encoding encoding;
            try
            {
                encoding = !string.IsNullOrEmpty(charset)
                    ? Encoding.GetEncoding(charset)
                    : Encoding.UTF8;
            }
            catch
            {
                encoding = Encoding.UTF8;
            }

            // 2️⃣ Decode safely
            string html = encoding.GetString(bytes);

            // 3️⃣ Clean up whitespaces and invisible chars
            html = Regex.Replace(html, @"\s+", " ").Trim();

            return html;
        }
    }
}

