using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.MN.Core
{
    public class SpeechResult
    {
        public SpeechResult()
        {
            Predictions = new List<SpeechText> { };
        }
        public List<SpeechText> Predictions { get; set; }
        public string? Text { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
