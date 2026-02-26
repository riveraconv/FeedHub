using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FeedHub_Core.Utilities;

namespace FeedHub_App.ViewModels.News
{
    public partial class QuickViewViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _newsContent;

        [ObservableProperty]
        private string _speakIcon = "🔊";

        private readonly ILogger _logger;
        private CancellationTokenSource _speechCts;

        [RelayCommand]
        public void SpeakNews()
        {
            if (string.IsNullOrWhiteSpace(NewsContent)) return;

            if (_speechCts != null)
            {
                _logger?.Info("TTS: Solicitando parada...");
                StopSpeaking();
                return;
            }

            _speechCts = new CancellationTokenSource();
            var token = _speechCts.Token;
            SpeakIcon = "🔇";

            Task.Run(async () =>
            {
                try
                {
                    await TextToSpeech.Default.SpeakAsync(NewsContent, new SpeechOptions
                    {
                        Pitch = 1.0f,
                        Volume = 1.0f
                    }, token);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger?.Error($"Error TTS: {ex.Message}");
                }
                finally
                {
                    // Solo si el token que termina es el que nosotros lanzamos, reseteamos el icono
                    if (_speechCts?.Token == token)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            _speechCts = null;
                            SpeakIcon = "🔊";
                        });
                    }
                }
            }, token);
        }

        public void StopSpeaking()
        {
            if (_speechCts != null)
            {
                _speechCts.Cancel();
                _speechCts.Dispose();
                _speechCts = null;
            }
            SpeakIcon = "🔊";
        }

        public void OnDisappearing()
        {
            if (_speechCts != null)
            {
                StopSpeaking();
            }
        }
    }
}
