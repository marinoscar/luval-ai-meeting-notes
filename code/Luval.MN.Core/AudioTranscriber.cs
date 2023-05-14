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

        public SpeechResult GetText(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));

            var converter = new AudioFormatConverter(fileName, Logger);
            fileName = converter.Convert();

            Logger.LogInformation("Starting to work on file {0}", fileName);
            var speechConfig = SpeechConfig.FromSubscription(Config.Key, Config.Region);
            speechConfig.SpeechRecognitionLanguage = Config.Language;
            speechConfig.SetProperty("ConversationTranscriptionInRoomAndOnline", "true");

            using (var audioConfig = AudioConfig.FromWavFileInput(fileName))
            {
                var meetingID = Guid.NewGuid().ToString();
                using (var transcriber = new ConversationTranscriber(audioConfig))
                {
                    _stopRecognition = new TaskCompletionSource<int>();
                    _result = new SpeechResult();
                    _speechText = new Dictionary<string, SpeechText>();
                    RegisterTranscriptionEvents(transcriber);

                    Task.WaitAny(new[] { _stopRecognition.Task }, Config.Timeout);

                }
            }
            _result.Text = string.Join(Environment.NewLine, _speechText.Values.Select(i => i.Text));
            _result.Predictions = _speechText.Values.ToList();
            return _result;
        }

        #region Transcription Events
        private void RegisterTranscriptionEvents(ConversationTranscriber transcriber)
        {
            transcriber.Transcribed += Transcriber_Transcribed;
            transcriber.Transcribing += Transcriber_Transcribing;
            transcriber.Canceled += Transcriber_Canceled;
            transcriber.SessionStarted += Transcriber_SessionStarted;
            transcriber.SessionStopped += Transcriber_SessionStopped;
            transcriber.SpeechEndDetected += Transcriber_SpeechEndDetected;
            transcriber.SpeechStartDetected += Transcriber_SpeechStartDetected;
        }

        private void Transcriber_SpeechStartDetected(object? sender, RecognitionEventArgs e)
        {
            Logger.LogInformation($"Session: {e.SessionId} - Event: SpeechStartDetected - Offset {e.Offset}");
        }

        private void Transcriber_SpeechEndDetected(object? sender, RecognitionEventArgs e)
        {
            Logger.LogInformation($"Session: {e.SessionId} - Event: SpeechEndDetected - Offset {e.Offset}");
        }

        private void Transcriber_SessionStopped(object? sender, SessionEventArgs e)
        {
            Logger.LogInformation($"Session: {e.SessionId} - Event: SessionStopped");
            _stopRecognition.TrySetResult(0);
        }

        private void Transcriber_SessionStarted(object? sender, SessionEventArgs e)
        {
            Logger.LogInformation($"Session: {e.SessionId} - Event: SessionStarted");
        }

        private void Transcriber_Canceled(object? sender, ConversationTranscriptionCanceledEventArgs e)
        {
            Logger.LogInformation($"Session: {e.SessionId} - Event: Canceled - Reason: {e.Reason} - Offset {e.Offset}");
            if (e.Reason == CancellationReason.Error)
                Logger.LogError($"ERROR ON Session: {e.SessionId} - Event: Canceled - Reason: {e.Reason} - Error Code: {e.ErrorCode} - Error Details: {e.ErrorDetails} - Offset {e.Offset}");
            _stopRecognition.TrySetResult(0);
        }

        private void Transcriber_Transcribing(object? sender, ConversationTranscriptionEventArgs e)
        {
            Logger.LogInformation($"Session: {e.SessionId} - Event: Transcribing - Characters Processed: {e.Result.Text.Length} - Offset {e.Offset}");
        }

        private void Transcriber_Transcribed(object? sender, ConversationTranscriptionEventArgs e)
        {
            Logger.LogInformation($"Session: {e.SessionId} - Event: Transcribed - Characters Processed: {e.Result.Text.Length} - Result Id {e.Result.ResultId} - User Id {e.Result.UserId} - Offset {e.Offset}");
            if (e.Result == null) return;
            _speechText[e.Result.ResultId] = new SpeechText()
            {
                Id = e.Result.ResultId,
                SessionId = e.SessionId,
                Duration = e.Result.Duration,
                Text = e.Result.Text,
                Confidence = 1d,
                SpeakerId = e.Result.UserId,
                ExtendedProperties = new Dictionary<string, object> {
                    { "UtteranceId", e.Result.UtteranceId },
                    { "OffsetInTicks", e.Result.OffsetInTicks },
                    { "Reason", e.Result.Reason.ToString() },
                }
            };
        }
        #endregion
    }
}