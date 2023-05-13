using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.MN.Core
{
    public class Speech2TextConfig
    {
        public Speech2TextConfig()
        {
            Region = "southcentralus";
            Language = "en-US";
            Timeout = TimeSpan.FromMinutes(15);
        }

        public string? Key { get;  set; }
        public string? Region { get;  set; }
        public string? Language { get;  set; }
        public TimeSpan Timeout { get; set; }
    }
}
