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
        public string Text { get; set; } = string.Empty;
    }

    public class QuickArticleService
    {
        private readonly string[] _stopPatterns = {
                                "Ver comentarios",
                                "Lo más leído",
                                "Lo último",
                                "Temas:",
                                "Noticias relacionadas",
                                "Sigue leyendo",
                                "Más información",
                                "Comentarios"
                            };
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

            // Text

            string plainText = Regex.Replace(textContent, "<.*?>", string.Empty);


            return new QuickArticleResult
            {
                Html = textContent,
                Title = titleText,
                ImageUrl = imageUrl,
                Text = plainText
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

            // 2️⃣ Normalización de bloques: Convertimos etiquetas estructurales en SALTOS DE LÍNEA (\n)
            // Esto es vital para que "Alberto Martín" y "El mundo tiene ganas..." no acaben en la misma línea.
            html = Regex.Replace(html, @"(?i)</?(address|blockquote|div|h[1-6]|li|p|section|article|aside|header|footer|figcaption|time|span)>", "\n");
            html = Regex.Replace(html, @"(?i)<br\s*/?>", "\n");

            // 3️⃣ Limpieza de etiquetas: Borramos todo EXCEPTO formato básico (opcional, según tu UI)
            // He quitado los enlaces (<a>) porque en móviles suelen dar problemas de clics accidentales
            html = Regex.Replace(html, @"<(?!/?(b|strong|i|em)\b)[^>]*>", "");

            // 4️⃣ Separamos por líneas reales
            var rawLines = html.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // 6️⃣ Filtrar palabras de spam sobre cada línea
            string[] noisePatterns = {
            "síguenos", "suscríbete", "publicidad", "te puede interesar",
            "leer más", "comparte", "newsletter", "tweet", "facebook", "compartir",
            "suscripción", "regístrate", "pago", "premium",
            "añadir usuario", "continuar leyendo", "leer más en", "solo para suscriptores",
            "inicia sesión", "tu cuenta", "tu suscripción", "compartir en",
            "comentarios", "cambiar tu contraseña", "modo premium", "accede aquí", "Por qué estás viendo esto?", "Por qué confiar en el Periódico",
            "Cada uno accederá con su propia cuenta de email, lo que os permitirá personalizar vuestra experiencia en el PAÍS.",
            "Amazon", "PcComponentes", "El Corte Inglés", "MediaMarkt", "eBay",
            "Clic para ver", "Hoy en", "oferta", "El precio podría variar. Obtenemos comisión por estos enlaces",
            "Basado en hechos observados y verificados directamente por nuestros periodistas o por fuentes informadas.",
            "Si continúas leyendo en este dispositivo, no se podrá leer en el otro.", "Rellena tu nombre y apellido para comentar",
            "Please enable JavaScript to view the comments powered by Disqus.", "RRSS WhatsApp", "RRSS Twitter", "RRSS email",
            "¿Por qué confiar en nosotros?", "¿Quieres ayudarnos?", "con amig@s, colegas... que puedan estar interesad@s.",
            "Y si aún no lo recibes, te puedes sumar a nuestra lista de correo aquí", "Hacemos EFEVerde", "Y recuerda que puedes recibir",
            "notificaciones en nuestra app. Descarga la última versión y actívalas.", "añade el mundo en google",
            "haz que nuestras noticias aparezcan en tus búsquedas.", "márcanos como medio preferente",
            "añádenos en google.", "elígenos como tu fuente preferida en google.", "información del artículo",
            "autor", "tenemos una nueva app", "añade eltiempo.es a tus medios preferidos en google.",
            "el audio de esta noticia está generado por inteligencia artificial."
            };

            var cleanParagraphs = new List<string>();
           
            foreach (var line in rawLines)
            {
                var trimmedLine = line.Trim();

                //0) Freno de mano, si la línea contiene stopPaterns, el artículo termina ahí
                if (_stopPatterns.Any(p => trimmedLine.Contains(p, StringComparison.OrdinalIgnoreCase)))
                {
                    break;
                }

                // 🟢 FILTRO DE CALIDAD 🟢

                // A) Demasiado corto (Metadatos como "Editor", "Fecha", "12")
                if (trimmedLine.Length < 25) continue;

                // B) Si es una línea de "Spam" conocida
                if (noisePatterns.Any(p => trimmedLine.Contains(p, StringComparison.OrdinalIgnoreCase))) continue;

                // C) Si la línea es puramente un enlace o texto de red social
                if (trimmedLine.StartsWith("http") || trimmedLine.Contains("pic.twitter")) continue;

                // D) Si la línea contiene anuncions con precios etc
                if (Regex.IsMatch(trimmedLine, @"\d+[,.]\d+\s*(€|euros)", RegexOptions.IgnoreCase)) continue;

                

                // Línea corta y nombres de tiendas
                if (trimmedLine.Length < 30 && noisePatterns.Any(p => trimmedLine.Contains(p, StringComparison.OrdinalIgnoreCase))) continue;

                // Limpiamos espacios dobles internos
                var finalLine = Regex.Replace(trimmedLine, @"\s+", " ");

                // Añadimos como párrafo (sin forzar el punto al final, que el autor escriba bien)
                cleanParagraphs.Add($"<p>{finalLine}</p>");
            }

            // 6️⃣ Reconstrucción
            return string.Join("\n", cleanParagraphs);
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
                doc.DocumentNode.SelectSingleNode("//div[contains(@class,'main-content')]") ??
                doc.DocumentNode.SelectSingleNode("//div[contains(@class,'content-inner')]") ??
                doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'ep-article-body')]") ??

                //general standars
                doc.DocumentNode.SelectSingleNode("//div[@itemprop='articleBody']") ??
                doc.DocumentNode.SelectSingleNode("//article") ??
                doc.DocumentNode.SelectSingleNode("//main") ??
                GetBestContentNode(doc.DocumentNode);


            if (articleNode == null) return null;

            var unwantedSelectors = new[]
            {
                ".//aside", "//footer", "//nav", "//header", "//script", "//style",
                ".//*[contains(@class,'ed-sections')]",  // El menú azul de la imagen 1
                ".//*[contains(@class,'site-map')]",
                ".//*[contains(@class,'related')]",     // Bloques de "Te puede interesar"
                ".//*[contains(@class,'featured-related')]", // Noticias sugeridas (Imagen 2 y 3)
                ".//*[contains(@class,'ad-')]",
                ".//*[contains(@class,'video-player')]", // Vídeos que rompen el texto
                ".//*[contains(@class,'m-entry-slot')]", // "Te puede interesar" interno
                ".//*[contains(@class,'social-share')]",
                ".//div[contains(@id, 'js-article-video')]",
                ".//div[contains(@class, 'ad-wrapper')]",
                ".//*[contains(@class,'newsletter')]",
                ".//*[contains(@class,'magazine-promo')]",
                ".//div[contains(@class, 'shop-product')]",  // Cajas de compra (Amazon, etc)
                ".//div[contains(@class, 'toc')]",
                ".//*[contains(@class, 'article-author')]",
                ".//*[contains(@class, 'author-info')]",
                ".//*[contains(@class, 'article-date')]",
                ".//*[contains(@class, 'article-metadata')]",
                ".//span[contains(@class, 'author')]",
                ".//figcaption",
                ".//*[contains(@class, 'caption')]",
                ".//*[contains(@class, 'social-embed-caption')]",
                ".//*[contains(@class, 'click-to-see')]",
                ".//*[contains(@class, 'news-list')]",       // Listas de noticias al pie
                ".//*[contains(@class, 'related-news')]",    // Relacionadas
                ".//*[contains(@class, 'ep-related-items')]", // El estándar de Prensa Ibérica (su grupo)
                ".//*[contains(@class, 'tags-container')]",  // Etiquetas/Tags que ensucian el final
                ".//section[contains(@class, 'recomended')]", // Recomendados
                ".//div[contains(@id, 'social-share-bottom')]", // Botones sociales finales
                ".//div[contains(@class, 'author-bio')]",     // Biografía del autor al pie
                ".//*[contains(@class, 'tp-comments')]",       // El botón de Ver comentarios
                ".//*[contains(@class, 'ep-related-articles')]", // Artículos relacionados
                ".//*[contains(@id, 'v-pinitos')]",            // El widget de "Lo más leído" (v-pinitos es común)
                ".//*[contains(@class, 'ep-ranking')]",        // El ranking de noticias
                ".//div[contains(@class, 'ep-more-news')]",    // Bloque de "Más noticias"
                ".//aside",                                    // Los periódicos meten casi todo el ruido en <aside>
                ".//div[@id='comments']",
                ".//*[contains(@class, 'ep-content-footer')]",
                ".//*[contains(@class, 'ep-section')]",
                ".//*[contains(@class, 'ep-common-ranking')]",
                ".//div[contains(@class, 'social-comments')]",
                ".//div[contains(@class, 'newsletter-subscription')]",
                ".//div[contains(@class, 'mod-more-news')]"
            };
            

            foreach (var selector in unwantedSelectors)
            {
                var nodes = articleNode.SelectNodes(selector)?.ToList();
                if (nodes != null) foreach (var n in nodes) n.Remove();
            }

            
            //Limpieza de párrafos que son solo links cortos
            var shortLinks = articleNode.SelectNodes(".//p[count(a) = 1 and string-length(text()) < 50]");
            if (shortLinks != null) foreach (var sl in shortLinks) sl.Remove();

            // 4️⃣ LIMPIEZA FINAL: Si después de todo, el nodo resultante sigue siendo 
            // mayoritariamente enlaces, buscamos dentro de él solo los párrafos.
            if (CalculateLinkDensity(articleNode) > 0.5)
            {
                // Si falló y pilló un menú, intentamos buscar solo los párrafos <p> 
                // que tengan una longitud decente dentro del nodo.
                var paragraphs = articleNode.SelectNodes(".//p");
                if (paragraphs != null)
                {
                    var cleanNode = HtmlNode.CreateNode("<div></div>");
                    foreach (var p in paragraphs)
                    {
                        var text = p.InnerText.Trim();
                        // Solo aceptamos párrafos con una longitud mínima y que no sean "StopPatterns"
                        if (text.Length > 40 && !_stopPatterns.Any(sp => text.Contains(sp, StringComparison.OrdinalIgnoreCase)))
                        {
                            cleanNode.AppendChild(p.Clone());
                        }
                    }
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

