using Microsoft.Maui.Handlers;

namespace FeedHub_App.Platforms.Android.Handlers
{
    public class CustomWebViewHandler : WebViewHandler
    {
        protected override global::Android.Webkit.WebView CreatePlatformView()
        {
            var webView = base.CreatePlatformView();
            webView.ScrollBarStyle = global::Android.Views.ScrollbarStyles.InsideOverlay;
            webView.HorizontalScrollBarEnabled = false;
            webView.VerticalScrollBarEnabled = false;
            return webView;
        }
    }
}


