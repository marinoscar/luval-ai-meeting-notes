using Luval.GPT.Agent.Core.Activity;
using Luval.OpenAI;
using Luval.OpenAI.Chat;
using Luval.OpenAI.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Luval.MN.Core.Activities
{
    public abstract class ChunkActivityBase : BaseActivity, ILLMActivity
    {
        public ILogger Logger { get; set; }
        public Func<ChatEndpoint> Chatendpoint { get; set; }

        protected virtual string Prompt { get; private set; }

        public ChunkActivityBase(ILogger logger, Func<ChatEndpoint> chatEndpoint, string prompt) : base(logger)
        {
            Logger = logger;
            Chatendpoint = chatEndpoint;
            Prompt = prompt;
        }

        public override bool ImplementListResult => false;

        public int TokensUsed { get; set; }

        protected async override Task OnExecuteAsync()
        {
            var text = InputParameters["Text"];
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentNullException(nameof(text), "The Text Input Parameter was not provided");
            LogInfo($"{Name}: Extracting chunks");
            var chunks = GetChunks(GetParagraphs(text));
            var results = new List<string>();
            foreach (var chunk in chunks)
            {
                var a = Create(chunk);
                await a.ExecuteAsync();
                results.Add(a.Result.Values.First());
                TokensUsed += a.TokensUsed;
                OnChunkPromptCompleted(chunk, a);
            }
            Result["Choice"] = string.Join(Environment.NewLine, results);
        }

        protected virtual void OnChunkPromptCompleted(string chunk, ILLMActivity activity)
        {

        }

        private ChatActivity Create(string text)
        {
            return  new ChatActivity(Logger, Chatendpoint(), Prompt, 0d) { 
                Name = "Runs Prompt on Chunk", Description = Description,
                InputParameters = new Dictionary<string, string> { { "Text", text } }
            };
        }

        private List<Paragraph> GetParagraphs(string text)
        {
            //var pattern = @"(?<=\n\n|^)([^\n]+)";
            var sw = Stopwatch.StartNew();
            var result = new List<Paragraph>();
            var paragraphs = Regex.Split(text, @"(\n|\.|(\n\n)|(\?\n)|(!\n))").Where(i => i.Length > 1).ToList();
            var sb = new StringBuilder();
            var index = 0;
            while (true)
            {
                if(index < paragraphs.Count)
                {
                    sb.Append(paragraphs[index]);
                    if (sb.Length > 1000)
                    {
                        var tokens = TokenCalculator.GetTokens(sb.ToString(), Model.GPTTurbo);
                        result.Add(new Paragraph() { Text = sb.ToString(), Tokens = tokens.Count });
                        sb.Clear();
                    }
                }
                else {
                    var tokens = TokenCalculator.GetTokens(sb.ToString(), Model.GPTTurbo);
                    result.Add(new Paragraph() { Text = sb.ToString(), Tokens = tokens.Count });
                    break;
                }
                index++;
            }
            sw.Stop();
            Debug.WriteLine(sw.Elapsed);
            return result;
        }

        private List<string> GetChunks(List<Paragraph> paragraphs)
        {
            var max = Math.Floor((4096 * 0.98));
            var chunkSize = Math.Floor(max * 0.8);
            var result = new List<string>();
            var tokenLoad = 0;
            var index = 0;
            var sb = new StringBuilder();
            while (true)
            {
                if (index < paragraphs.Count)
                {
                    tokenLoad += paragraphs[index].Tokens;
                    if (tokenLoad <= chunkSize)
                    {
                        sb.Append(paragraphs[index].Text);
                    }
                    else
                    {
                        sb.Append(paragraphs[index].Text);
                        result.Add(sb.ToString());
                        sb.Clear();
                        tokenLoad = 0;
                    }
                    index++;
                }
                else
                {
                    result.Add(sb.ToString());
                    break;
                }
            }
            return result;
        }


        private class Paragraph { public string Text { get; set; } public int Tokens { get; set; } }

    }
}
