using Luval.GPT.Agent.Core;
using Luval.GPT.Agent.Core.Activity;
using Luval.OpenAI.Chat;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.MN.Core.Activities
{
    public class AnalyzeTranscriptGPT4Activity : BaseActivity, ILLMActivity
    {
        public ILogger Logger { get; set; }
        public Func<ChatEndpoint> Chatendpoint { get; set; }
        public string TranscriptText { get; private set; }
        public override string Name { get; set; }
        public override string Description { get; set; }

        public override bool ImplementListResult => false;

        public AnalyzeTranscriptGPT4Activity(ILogger logger, Func<ChatEndpoint> chatEndpoint) : base(logger)
        {
            Logger = logger;
            Chatendpoint = chatEndpoint;
        }

        protected async override Task OnExecuteAsync()
        {
            var transcriptFile = InputParameters["TranscriptFileName"];
            if (transcriptFile == null) throw new ArgumentNullException(nameof(transcriptFile));
            if (!File.Exists(transcriptFile)) throw new FileNotFoundException("File not found", transcriptFile);

            TranscriptText = File.ReadAllText(transcriptFile);

            var audioFileName = InputParameters["AudioFileName"];
            if (audioFileName == null) throw new ArgumentNullException(nameof(audioFileName));
            if (!File.Exists(audioFileName)) throw new FileNotFoundException("File not found", audioFileName);

            var audioFile = new FileInfo(audioFileName);

            var actionItems = await GetActionItems();
            Thread.Sleep(1000 * 60);
            var summary = await GetSummary();
            Thread.Sleep(1000 * 60);
            var subject = await GetSubject();
            Thread.Sleep(1000 * 60);
            var result = new TranscriptAnalyzerResult()
            {
                Subject = subject,
                ActionItems = actionItems,
                Summary = summary,
                Transcript = TranscriptText
            };

            var name = audioFile.Name.Replace(audioFile.Extension, "");
            var jsonResult = JsonConvert.SerializeObject(result);
            var jsonFileName = Path.Combine(audioFile.DirectoryName, $"{name}-analyzer.json");
            var summaryFileName = Path.Combine(audioFile.DirectoryName, $"{name}-summary.txt");
            var actionItemsFileName = Path.Combine(audioFile.DirectoryName, $"{name}-action-items.txt");

            LogInfo($"Saving results");
            File.WriteAllText(jsonFileName, jsonResult);
            File.WriteAllText(summaryFileName, summary);
            File.WriteAllText(actionItemsFileName, actionItems);

            Result["Analyzer-Result"] = jsonFileName;
            Result["Analyzer-Summary"] = summaryFileName;
            Result["Analyzer-ActionItems"] = actionItemsFileName;

        }

        public int TokensUsed { get; set; }

        private async Task<string> GetSummary()
        {
            var task = CreateActivity("Get summary", PromptSummary,
                new Dictionary<string, string>() {
                    { "transcript", TranscriptText }
                });
            task.DelayBetweenRetries = TimeSpan.FromMinutes(3);
            await task.ExecuteAsync();
            TokensUsed += task.TokensUsed;
            return task.Result.Values.FirstOrDefault();
        }

        private async Task<string> GetActionItems()
        {
            var task = CreateActivity("Get action items", PromptActionItems,
                new Dictionary<string, string>() {
                    { "transcript", TranscriptText }
                });
            task.DelayBetweenRetries = TimeSpan.FromMinutes(3);
            await task.ExecuteAsync();
            TokensUsed += task.TokensUsed;
            return task.Result.Values.FirstOrDefault();
        }

        private async Task<string> GetSubject()
        {
            var task = CreateActivity("Get subject", PromptSubject,
                new Dictionary<string, string>() {
                    { "transcript", TranscriptText }
                });
            task.DelayBetweenRetries = TimeSpan.FromMinutes(3);
            await task.ExecuteAsync();
            TokensUsed += task.TokensUsed;
            return task.Result.Values.FirstOrDefault();
        }

        private ChatActivity CreateActivity(string name, string prompt, Dictionary<string, string> parameters)
        {
            return new ChatActivity(Logger, Chatendpoint(), prompt, 0d) { Name = name, InputParameters = parameters };
        }

        private string PromptSummary = @"
Provide a summary of the transcript, exclude small talk and focus on business related content, keep information that can be used as action items or tasks, maintain names, locations, systems and other important information in the business settings for this transcript
{transcript}
";
        private string PromptActionItems = @"
Provide the action items for the following transcript
{transcript}
";

        private string PromptSubject = @"
Provide what is the subject for this transcript
{transcript}
";



    }
}
