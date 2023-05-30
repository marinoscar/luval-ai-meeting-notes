using Luval.GPT.Agent.Core.Activity;
using Luval.OpenAI.Chat;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.MN.Core.Activities
{
    public class SummarizeActivity : ChunkActivityBase
    {
        public SummarizeActivity(ILogger logger, Func<ChatEndpoint> chatEndpoint) : base(logger, chatEndpoint, GetPrompt())
        {
            Name = "Summarizes the text";
            Description = Name;
        }

        private static string GetPrompt() {
            return @"
Provide a detailed summary the following text, make sure to keep all relevant data for action items, names, places and dates and provide nothing but the summary and focus only on the content
Here is the content to sumarize:
{Text}
";
        }

        public override string Name { get; set; }
        public override string Description { get; set; }
    }
}
