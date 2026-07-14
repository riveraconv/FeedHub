using FeedHub_Core.Models;

namespace FeedHub_Core.Services
{
    public class AdInterleaveService
    {
        private const int AdEvery = 5;
        public List<object> Interleave(IEnumerable<NewsItem> items)
        {
            var result = new List<object>();
            int count = 0;

            foreach (var item in items)
            {
                result.Add(item);
                count++;

                if (count % AdEvery == 0)
                {
                    var ad = new AdItem();
                    System.Diagnostics.Debug.WriteLine($"DEBUG INTERLEAVE: añadiendo AdItem en posición {count}");
                     result.Add(ad);
                }
                   
            }
            System.Diagnostics.Debug.WriteLine($"DEBUG INTERLEAVE: total={result.Count}, ads={result.OfType<AdItem>().Count()}");
            return result;
        }
    }
}
