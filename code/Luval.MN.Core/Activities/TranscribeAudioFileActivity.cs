using Luval.GPT.Agent.Core;
using Luval.GPT.Agent.Core.Activity;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.MN.Core.Activities
{
    public class TranscribeAudioFileActivity : BaseActivity
    {

        private AudioFormatConverter _converter;
        private AudioTranscriber _transcriber;

        public TranscribeAudioFileActivity(ILogger logger, AudioTranscriber audioTranscriber, AudioFormatConverter audioFormatConverter) : base(logger)
        {
            _converter = audioFormatConverter ?? throw new ArgumentNullException(nameof(audioFormatConverter));
            _transcriber = audioTranscriber ?? throw new ArgumentNullException(nameof(audioTranscriber)); ;
            Name = "Transcribe Audio Files";
        }

        public override string Name { get; set; }
        public override string Description { get; set; }

        public override bool ImplementListResult => false;

        protected override Task OnExecuteAsync()
        {
            return Task.Run(() => { RunSync(); });
        }

        private void RunSync()
        {
            var fnoEx = _converter.AudioFile.Name.Replace(_converter.AudioFile.Extension, "");
            LogInfo($"Converting file {_converter.AudioFile}");
            
            var convertedFile = ConvertFile();
            File.Move(_converter.AudioFile.FullName, Path.Combine(convertedFile.Directory.FullName, _converter.AudioFile.Name));

            LogInfo($"Transcribing file {convertedFile.FullName}");
            var result = TranscribingFile(convertedFile.FullName);
            LogInfo($"Saving Results to{convertedFile.DirectoryName}");
            var resultJson = JsonConvert.SerializeObject(result);

            var transcriptFile = Path.Combine(convertedFile.DirectoryName, $"{fnoEx}-transcript.txt");
            var resultFile = Path.Combine(convertedFile.DirectoryName, $"{fnoEx}-result.json");
            File.WriteAllText(resultFile, resultJson);
            File.WriteAllText(transcriptFile, result.Text);

            Result["ConvertedAudio"] = convertedFile.FullName;
            Result["ResultFile"] = resultFile;
            Result["TranscriptFile"] = transcriptFile;
        }

        private SpeechResult TranscribingFile(string audioFile)
        {
            return _transcriber.TransacribeAsync(audioFile).Result;
        }

        private FileInfo ConvertFile()
        {
            var file = _converter.AudioFile;
            var destinationFolder = InputParameters["DestinationFolder"];
            if (string.IsNullOrEmpty(destinationFolder)) throw new ArgumentException($"Directory not provided");
            if (!file.Exists) throw new ArgumentException($"File {file.FullName} doesn't exists");

            if (!Directory.Exists(destinationFolder)) Directory.CreateDirectory(destinationFolder);

            var destinationFile = new FileInfo(Path.Combine(destinationFolder, file.Name.Replace(file.Extension, ".wav")));
            _converter.Convert(destinationFile.FullName);
            return destinationFile;
        }
    }
}
