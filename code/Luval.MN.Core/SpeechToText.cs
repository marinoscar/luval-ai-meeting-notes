using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using System.Security;

namespace Luval.MN.Core
{
    public class SpeechToText
    {

        public SpeechToText(Speech2TextConfig config, ILogger logger)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Logger = logger;
        }

        private List<SpeechFragment> _result;

        public Speech2TextConfig Config { get; private set; }
        public ILogger Logger { get; private set; }

        public List<SpeechFragment> GetText(string fileName)
        {
            var speechConfig = SpeechConfig.FromSubscription(Config.Key, Config.Region);
            speechConfig.SpeechRecognitionLanguage = Config.Language;
            using (var audioConfig = AudioConfig.FromWavFileInput(fileName))
            {
                using (var speech = new SpeechRecognizer(speechConfig, audioConfig))
                {
                    _result = new List<SpeechFragment>();
                    speech.Recognizing += Speech_Recognizing;
                    var t = speech.StartContinuousRecognitionAsync();
                    t.Wait(Config.Timeout);
                }
            }
            return _result;
        }

        private void Speech_Recognizing(object? sender, SpeechRecognitionEventArgs e)
        {
            _result.Add(new SpeechFragment() { Text = e.Result.Text, Duration = e.Result.Duration });
            Logger?.LogInformation(e.Result.Text);
        }
    }
}