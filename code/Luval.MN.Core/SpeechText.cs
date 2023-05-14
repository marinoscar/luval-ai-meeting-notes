using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.MN.Core
{
    public class SpeechText
    {
        public SpeechText()
        {
            ExtendedProperties = new Dictionary<string, object>();
        }

        public string? Id { get; set; }
        public string? SessionId { get; set; }
        public string? Text { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? SpeakerId { get; set; }
        public double? Confidence { get; set; }

        public Dictionary<string, object> ExtendedProperties { get; set; }
    }
}
