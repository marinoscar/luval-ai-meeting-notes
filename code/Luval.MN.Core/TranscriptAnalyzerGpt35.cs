using Luval.OpenAI.Chat;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.MN.Core
{
    public class TranscriptAnalyzerGpt35
    {
        public ILogger Logger { get; private set; }
        public string? Transcript { get; private set; }
        public int OriginalTokenCount { get; private set; }

        protected virtual ChatEndpoint Endpoint { get; private set; }


        public TranscriptAnalyzerGpt35(ILogger logger, string? transcript, ChatEndpoint chatEndpoint)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Transcript = transcript ?? throw new ArgumentNullException(nameof(transcript));
            Endpoint = chatEndpoint ?? throw new ArgumentNullException(nameof(chatEndpoint));
        }

        public async Task<TranscriptAnalyzerResult> ExecuteAsync()
        {
            Logger.LogInformation("Starting to process the transcript");
            var summary = string.Join(Environment.NewLine, await ExtractSummary());
            var actionItems = await ExtractActionItems(summary);
            var subject = await ExtractSubject(summary);
            return new TranscriptAnalyzerResult()
            {
                Summary = summary, ActionItems = actionItems, Subject = subject, Transcript = Transcript
            };
        }

        private async Task<List<string>> ExtractSummary()
        {
            var summary = new List<string>();
            var chunks = ChunkText(Transcript);
            foreach (var chunk in chunks)
            {
                var idx = chunks.IndexOf(chunk) + 1;
                Logger.LogInformation($"Getting the main ideas for chunk {idx}");
                var p = PromptMainIdeas.Replace("{transcript}", chunk);
                var r = await TryRunPrompt(p);
                if (r == null) continue;
                Logger.LogInformation($"Main ideas prompt completed for {idx} of {chunks.Count}");
                summary.Add(r.Choice.Message.Content);
            }
            return summary;
        }

        private async Task<string> ExtractActionItems(string content)
        {
            Logger.LogInformation($"Getting the action items");
            var p = PromptActionItems.Replace("{transcript}", content);
            var r = await TryRunPrompt(p);
            if (r == null) return null;
            return r.Choice.Message.Content;
        }

        private async Task<string> ExtractSubject(string content)
        {
            Logger.LogInformation($"Getting the subject");
            var p = PromptSubject.Replace("{transcript}", content);
            var r = await TryRunPrompt(p);
            if (r == null) return null;
            return r.Choice.Message.Content;
        }

        private async Task<ChatResponse> TryRunPrompt(string p)
        {
            var success = false;
            var tries = 0;
            ChatResponse result = null;
            Endpoint.AddUserMessage(p);
            while (!success)
            {
                try
                {
                    result = await Endpoint.SendAsync(0d);
                    success = true;
                }
                catch (Exception ex)
                {
                    if (tries >= 3)
                    {
                        Logger.LogError($"Failed to run prompt. Exception: {ex}");
                        return null;

                    }
                    else
                        Logger.LogWarning($"Retrying to run prompt. Try #{tries + 1} Message: {ex.Message}");
                    tries++;
                }
            }
            Endpoint.ClearMessages();
            return result;
        }

        private List<string> ChunkText(string text)
        {
            var chunkSize = 4500;
            var paragraphs = GetParragraphs(text);
            var result = new List<string>();
            var sb = new StringBuilder();
            foreach (var paragraph in paragraphs)
            {
                sb.AppendLine(paragraph);
                if (sb.Length > chunkSize)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
            }
            return result;
        }

        private List<string> GetParragraphs(string text)
        {
            return text.Split(Environment.NewLine.ToCharArray()).ToList();
        }

        private string PromptMainIdeas = @"
Provide the main ideas, exclude small talk and focus on business related content, keep information that can be used as action items or tasks, maintain names, locations, systems and other important information in the business settings for this transcript
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
