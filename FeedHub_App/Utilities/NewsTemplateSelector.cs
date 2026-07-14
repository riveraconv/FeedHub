using FeedHub_Core.Models;
using Microsoft.Maui.Controls;

namespace FeedHub_App.Utilities;

    public class NewsTemplateSelector : DataTemplateSelector
    {
        public DataTemplate NewsTemplate { get; set; }
        public DataTemplate AdTemplate { get; set; }
        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
             System.Diagnostics.Debug.WriteLine($"DEBUG SELECTOR: tipo={item.GetType().Name}");
            return item is AdItem ? AdTemplate : NewsTemplate;
        }
    }
