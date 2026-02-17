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
            if (string.IsNullOrWhiteSpace(rawHtml)) return EmptyResult();

            var doc = new HtmlDocument();
            doc.LoadHtml(rawHtml);

            // 🔹 Título
            var titleNode = doc.DocumentNode.SelectSingleNode("//h1") ??
                            doc.DocumentNode.SelectSingleNode("//title");

            var titleText = titleNode?.InnerText.Trim() ?? "Sin título";

            // 🔹 Texto del artículo
            var articleNode = GetCleanArticleNode(doc);
            if (articleNode == null) return EmptyResult();

            // Image
            var imageUrl = ExtractBestImage(doc, articleNode) ?? string.Empty;

            string textContent = CleanText(articleNode.InnerHtml);
            textContent = RemoveResidualGarbage(textContent);

            // 🔹 Imagen principal (robusta)
            

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

            // Intentar OpenGraph primero (Suele ser la mejor calidad en Xataka)
            var ogImage = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']")?.GetAttributeValue("content", null);
            if (IsValidImageUrl(ogImage)) return ogImage;

            // Buscar en el contenido
            if (articleNode != null)
            {
                var img = articleNode.Descendants("img")
                    .Where(x => {
                        var src = x.GetAttributeValue("src", "") ?? x.GetAttributeValue("data-src", "");
                        return !src.Contains("avatar") && !src.Contains("icon") && !src.Contains("logo");
                    })
                    .FirstOrDefault();

                if (img != null)
                {
                    return img.GetAttributeValue("data-src", null) ??
                           img.GetAttributeValue("src", null);
                }
            }
            return null;
        }
        private bool IsValidImageUrl(string? url)
        {
            return !string.IsNullOrWhiteSpace(url) &&
                   !url.Contains("svg") &&
                   (url.StartsWith("http") || url.StartsWith("//"));
        }

        private string CleanText(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            html = WebUtility.HtmlDecode(html);

            //reemplaza cierre de bloques por nueva linea, no por espacio
            html = Regex.Replace(html, @"(?i)</(div|p|h[1-6]|li|blockquote|section|article)>", "\n");

            // 1️⃣ Quitar scripts, estilos y comentarios
            html = Regex.Replace(html, @"<script.*?>.*?</script>|<style.*?>.*?</style>|", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            // Reemplazar <br> por nueva línea
            html = Regex.Replace(html, @"(?i)<br\s*/?>", "\n");

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

        private QuickArticleResult EmptyResult()
        {
            return new QuickArticleResult
            {
                Html = "",
                Title = "",
                ImageUrl = ""
            };
        }

        private HtmlNode? GetCleanArticleNode(HtmlDocument doc)
        {
            if (doc.DocumentNode == null) return null;

            var unwantedSelectors = new[]
            {
                "//aside", "//footer", "//nav", "//header", "//script", "//style",
                "//*[contains(@class,'ed-sections')]",  // El menú azul de la imagen 1
                "//*[contains(@class,'site-map')]",
                "//*[contains(@class,'related')]",     // Bloques de "Te puede interesar"
                "//*[contains(@class,'featured-related')]", // Noticias sugeridas (Imagen 2 y 3)
                "//*[contains(@class,'ad-')]",
                "//*[contains(@class,'video-player')]", // Vídeos que rompen el texto
                "//*[contains(@class,'m-entry-slot')]", // "Te puede interesar" interno
                "//*[contains(@class,'social-share')]",
                "//div[contains(@id, 'js-article-video')]",
                "//div[contains(@class, 'ad-wrapper')]",
                "//*[contains(@class,'newsletter')]",
                "//*[contains(@class,'magazine-promo')]",
                "//div[contains(@class, 'shop-product')]",  // Cajas de compra (Amazon, etc)
                "//div[contains(@class, 'toc')]"
            };

            foreach (var selector in unwantedSelectors)
            {
                var nodes = doc.DocumentNode.SelectNodes(selector)?.ToList();
                if (nodes != null) foreach (var n in nodes) n.Remove();
            }

            // 1️⃣ Intentar selectores semánticos muy específicos primero
            // elDiario.es usa "article-text" para el cuerpo real
            var articleNode =
                doc.DocumentNode.SelectSingleNode("//div[contains(@class,'blob-container')]") ??
                doc.DocumentNode.SelectSingleNode("//div[@itemprop='articleBody']") ??
                doc.DocumentNode.SelectSingleNode("//div[contains(@class,'article-text')]") ??
                doc.DocumentNode.SelectSingleNode("//div[contains(@class,'article-content')]") ??
                doc.DocumentNode.SelectSingleNode("//div[contains(@class,'post-content')]") ??
                doc.DocumentNode.SelectSingleNode("//div[contains(@class,'article-content')]") ??
                doc.DocumentNode.SelectSingleNode("//div[contains(@class,'post-content')]") ??
                doc.DocumentNode.SelectSingleNode("//div[contains(@id,'main-content')]") ??
                //general standars
                doc.DocumentNode.SelectSingleNode("//div[@itemprop='articleBody']") ??
                doc.DocumentNode.SelectSingleNode("//article") ??
                doc.DocumentNode.SelectSingleNode("//main") ??
                GetBestContentNode(doc.DocumentNode);


            if (articleNode == null) return null;

            var shortLinks = articleNode.SelectNodes(".//p[count(a) = 1 and string-length(text()) < 50]");
            if (shortLinks != null) foreach (var sl in shortLinks) sl.Remove();

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
                //limpia links
                var linkOnlyParagraphs = articleNode.SelectNodes(".//p[count(a)=1 and string-length(text()) < 60]");
                if (linkOnlyParagraphs != null)
                {
                    foreach (var p in linkOnlyParagraphs) p.Remove();
                }
            }

            return articleNode;
        }
        private string RemoveResidualGarbage(string html)
        {
            // Limpieza final de copyright
            return Regex.Replace(html, @"(Copyright|Derechos reservados|©).{0,50}</p>", "", RegexOptions.IgnoreCase);
        }

        // Mantén tu GetBestContentNode y CalculateLinkDensity como estaban, 
        // son buenos "fallback" si fallan los selectores principales.
        private HtmlNode? GetBestContentNode(HtmlNode root)
        {
            return root.Descendants()
                .Where(n => (n.Name == "div" || n.Name == "section") && n.HasChildNodes)
                .OrderByDescending(n => {
                    var ps = n.SelectNodes("./p");
                    if (ps == null) return 0;
                    int score = ps.Count(p => p.InnerText.Trim().Length > 50) * 100;
                    if (CalculateLinkDensity(n) > 0.5) score -= 1000;
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

