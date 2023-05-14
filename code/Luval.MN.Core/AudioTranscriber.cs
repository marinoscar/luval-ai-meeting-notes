using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Security;
using System.Text.Json.Serialization;

namespace Luval.MN.Core
{
    public class AudioTranscriber
    {

        public AudioTranscriber(Speech2TextConfig config, ILogger logger)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Logger = logger;
        }

        private SpeechResult _result;
        private Dictionary<string, SpeechText> _speechText;
        private TaskCompletionSource<int> _stopRecognition;

        public Speech2TextConfig Config { get; private set; }
        public ILogger Logger { get; private set; }

        public async Task<SpeechResult> TransacribeAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));

            var converter = new AudioFormatConverter(fileName, Logger);
            fileName = converter.Convert();

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

                    await speech.StartContinuousRecognitionAsync();

                    Task.WaitAny(new[] { _stopRecognition.Task }, Config.Timeout);

                    await speech.StopContinuousRecognitionAsync();
                }
            }
            _result.Text = string.Join(Environment.NewLine, _speechText.Values.Select(i => i.Text));
            _result.Predictions = _speechText.Values.ToList();
            return _result;
        }


        #region Audio Events
        private void RegisterEvents(SpeechRecognizer speech)
        {
            speech.Recognizing += Speech_Recognizing; ;
            speech.Recognized += Speech_Recognized;
            speech.SessionStopped += Speech_SessionStopped;
            speech.SessionStarted += Speech_SessionStarted;
            speech.SpeechEndDetected += Speech_SpeechEndDetected;
            speech.SpeechStartDetected += Speech_SpeechStartDetected;
            speech.Canceled += Speech_Canceled;
        }

        private void Speech_Recognizing(object? sender, SpeechRecognitionEventArgs e)
        {
            Logger.LogDebug($"Session Id: {e.SessionId} Event: Recognized - Result Id: {e.Result.ResultId} - Characters: {e.Result.Text.Length} Reason: {e.Result.Reason} - Offset {e.Offset}");
        }

        private void Speech_Canceled(object? sender, SpeechRecognitionCanceledEventArgs e)
        {
            Logger.LogInformation($"Session Id: {e.SessionId} Event: Canceled - Reason: {e.Reason} - Offset {e.Offset}");

            if (e.Reason == CancellationReason.Error)
                Logger.LogError($"ERROR: Session Id: {e.SessionId} Error Code: {e.ErrorCode} Error Reason {e.ErrorDetails}");
            _stopRecognition.TrySetResult(0);
        }

        private void Speech_SpeechStartDetected(object? sender, RecognitionEventArgs e)
        {
            Logger.LogInformation($"Session Id: {e.SessionId} Event: SpeechStartDetected - Offset {e.Offset}");
        }

        private void Speech_SpeechEndDetected(object? sender, RecognitionEventArgs e)
        {
            Logger.LogInformation($"Session Id: {e.SessionId} Event: SpeechEndDetected - Offset {e.Offset}");
        }

        private void Speech_SessionStarted(object? sender, SessionEventArgs e)
        {
            Logger.LogInformation($"Session Id: {e.SessionId} Event: SessionStarted");
        }

        private void Speech_SessionStopped(object? sender, SessionEventArgs e)
        {
            Logger.LogInformation($"Session Id: {e.SessionId} Event: SessionStopped");
            _stopRecognition.TrySetResult(0);
        }

        private void Speech_Recognized(object? sender, SpeechRecognitionEventArgs e)
        {
            Logger.LogDebug($"Session Id: {e.SessionId} Event: Recognized - Result Id: {e.Result.ResultId} - Characters: {e.Result.Text.Length} Reason: {e.Result.Reason} - Offset {e.Offset}");
            if (e.Result == null) return;
            _speechText[e.Result.ResultId] = new SpeechText()
            {
                Id = e.Result.ResultId,
                SessionId = e.SessionId,
                Duration = e.Result.Duration,
                Text = e.Result.Text,
                Confidence = 1d,
                ExtendedProperties = new Dictionary<string, object> 
                {
                    { "Offset", e.Offset },
                    { "OffsetInTicks", e.Result.OffsetInTicks },
                    { "Properties", e.Result.Properties },
                }
            };
        }
        #endregion
    }
}