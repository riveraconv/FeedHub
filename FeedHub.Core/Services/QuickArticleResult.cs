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

            return new QuickArticleResult
            {
                Html = textContent,
                Title = titleText,
                ImageUrl = imageNode?? string.Empty
            };
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

