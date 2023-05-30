using Luval.GPT.Agent.Core;
using Luval.MN.Core.Activities;
using Luval.OpenAI.Chat;
using Luval.OpenAI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Luval.OpenAI.Models;
using Luval.GPT.Agent.Core.Activity;
using Luval.GPT.Agent.Core.Data;

namespace Luval.MN.Core.Agent
{
    public class MeetingNotesAgent : BaseAgent
    {

        private IActivity audioFiles;
        private IActivity transcribe;
        private ILLMActivity analyzer;

        public MeetingNotesAgent(ILogger logger) : base(logger, new AgentRepository())
        {
            Name = "Meeting Notes Agent";
            Description = "Creates meeting notes from an audio file";
        }
        protected async override Task OnExecuteAsync()
        {
            var speechConfig = new Speech2TextConfig() { Key = InputParameters["SpeechKey"] };
            var create = () =>
            {
                //var ai = ChatEndpoint.CreateAzure(
                //new ApiAuthentication(new NetworkCredential("", InputParameters["OpenAIKey"]).SecurePassword), "ey-sandbox", Model.Gpt432k.Id);
                //ai.Model = Model.Gpt432k;
                var ai = ChatEndpoint.CreateAzure(
                new ApiAuthentication(new NetworkCredential("", InputParameters["OpenAIKey"]).SecurePassword), "ey-sandbox");
                return ai;
            };

            audioFiles = new FindAudioFilesActivity(Logger);
            audioFiles.InputParameters["WorkingDirectory"] = InputParameters["WorkingDirectory"];
            audioFiles.InputParameters["DestinationFolder"] = InputParameters["DestinationFolder"];
            await RunActivity(audioFiles);

            foreach (var audioFile in audioFiles.Result.Values)
            {
                var transcriber = new TranscribeAudioFileActivity(Logger, new AudioTranscriber(speechConfig, Logger), new AudioFormatConverter(audioFile, Logger));
                transcriber.InputParameters["WorkingDirectory"] = InputParameters["WorkingDirectory"];
                transcriber.InputParameters["DestinationFolder"] = InputParameters["DestinationFolder"];
                await RunActivity(transcriber);

                Result["ConvertedAudioFile"] = transcriber.Result["ConvertedAudio"];
                Result["ResultAudioFile"] = transcriber.Result["ResultFile"];
                Result["TranscriptFile"] = transcriber.Result["TranscriptFile"];


                var text = File.ReadAllText(transcriber.Result["TranscriptFile"]);
                var summarizer = new SummarizeActivity(Logger, create);
                var actionItems = new ActionItemsActivity(Logger, create);
                var summaryFile = await RunChunkActivities(new FileInfo(audioFile), summarizer, text, "summary.txt");
                var actionFile = await RunChunkActivities(new FileInfo(audioFile), actionItems, text, "actions.txt");

                Result["SummaryFile"] = summaryFile;
                Result["ActionItemFile"] = actionFile;


                //var analyzer = new AnalyzeTranscriptGPT4Activity(Logger, create);
                //analyzer.InputParameters["AudioFileName"] = transcriber.Result["ConvertedAudio"];
                //analyzer.InputParameters["TranscriptFileName"] = transcriber.Result["TranscriptFile"];
                //await RunActivity(analyzer);
            }
        }

        private async Task<string> RunChunkActivities(FileInfo audioFile, ChunkActivityBase chunkActivity, string text, string suffix)
        {
            var fileName = audioFile.Name.Replace(audioFile.Extension, "") + "-" + suffix;
            var outputFile = Path.Combine(InputParameters["DestinationFolder"], fileName);
            chunkActivity.InputParameters["Text"] = text;
            await RunActivity(chunkActivity);
            File.WriteAllText(outputFile, chunkActivity.Result.Values.First());
            return outputFile;

        }
    }
}
