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
    public class ActionItemsActivity : ChunkActivityBase
    {
        public ActionItemsActivity(ILogger logger, Func<ChatEndpoint> chatEndpoint) : base(logger, chatEndpoint, GetPrompt())
        {
            Name = "Extract action items";
            Description = Name;
        }

        private static string GetPrompt()
        {
            return @"
Provide a detailed action items, do not number the action items, start the action items with the * character, just provide the paragraph with the action item and nothing more
Here is the content to use:
{Text}
";
        }

        public override string Name { get; set; }
        public override string Description { get; set; }
    }
}
