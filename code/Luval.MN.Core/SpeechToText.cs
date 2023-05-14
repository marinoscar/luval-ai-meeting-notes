using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using System.Security;
using System.Text.Json.Serialization;

namespace Luval.MN.Core
{
    public class SpeechToText
    {

        public SpeechToText(Speech2TextConfig config, ILogger logger)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Logger = logger;
        }

        private SpeechResult _result;
        private Dictionary<string, SpeechText> _speechText;
        private TaskCompletionSource<int> _stopRecognition;

        public Speech2TextConfig Config { get; private set; }
        public ILogger Logger { get; private set; }

        public SpeechResult GetText(string fileName)
        {
            Logger.LogInformation("Starting to work on file {0}", fileName);
            var speechConfig = SpeechConfig.FromSubscription(Config.Key, Config.Region);
            speechConfig.SpeechRecognitionLanguage = Config.Language;
            using (var audioConfig = AudioConfig.FromWavFileInput(fileName))
            {
                using (var speech = new SpeechRecognizer(speechConfig, audioConfig))
                {
                    _stopRecognition = new TaskCompletionSource<int>();
                    _result = new SpeechResult();
                    _speechText = new Dictionary<string, SpeechText>();
                    RegisterEvents(speech);
                    var t = speech.StartContinuousRecognitionAsync();
                    Task.WaitAny(new[] { _stopRecognition.Task }, Config.Timeout);
                }
            }
            _result.Text = string.Join(Environment.NewLine, _speechText.Values.Select(i => i.Text));
            _result.Predictions = _speechText.Values.ToList();
            return _result;
        }

        private void RegisterEvents(SpeechRecognizer speech)
        {
            speech.Recognizing += Speech_Recognizing;
            speech.Recognized += Speech_Recognized;
            speech.SessionStopped += Speech_SessionStopped;
            speech.SessionStarted += Speech_SessionStarted;
            speech.SpeechEndDetected += Speech_SpeechEndDetected;
            speech.SpeechStartDetected += Speech_SpeechStartDetected;
            speech.Canceled += Speech_Canceled;
        }

        private void Speech_Canceled(object? sender, SpeechRecognitionCanceledEventArgs e)
        {
            Logger.LogInformation($"Speech session canceled {e.SessionId}");
            if (e.Reason == CancellationReason.Error)
                Logger.LogError("Session: {0} Error Code: {1} Details: {2}", e.SessionId, e.ErrorCode, e.ErrorDetails);
            _stopRecognition.TrySetResult(0);
        }

        private void Speech_SpeechStartDetected(object? sender, RecognitionEventArgs e)
        {
            Logger.LogInformation($"Speech start detected Session {e.SessionId}");
        }

        private void Speech_SpeechEndDetected(object? sender, RecognitionEventArgs e)
        {
            Logger.LogInformation($"Speech end detected Session {e.SessionId}");
        }

        private void Speech_SessionStarted(object? sender, SessionEventArgs e)
        {
            Logger.LogInformation($"Speech session started {e.SessionId}");
        }

        private void Speech_SessionStopped(object? sender, SessionEventArgs e)
        {
            Logger.LogInformation($"Speech session stopped {e.SessionId}");
            _stopRecognition.TrySetResult(0);
        }

        private void Speech_Recognized(object? sender, SpeechRecognitionEventArgs e)
        {
            Logger.LogInformation($"Speech recognized {e.Result?.ResultId}");
            if (e.Result == null) return;
            _speechText[e.Result.ResultId] = new SpeechText() { Id = e.Result.ResultId, Duration = e.Result.Duration, Text = e.Result.Text, Confidence = 1d };
        }

        private void Speech_Recognizing(object? sender, SpeechRecognitionEventArgs e)
        {
            Logger?.LogDebug("Id: {0} Duration: {1} Text: {2}",e.Result?.ResultId,  e.Result?.Duration, e.Result?.Text);
        }
    }
}