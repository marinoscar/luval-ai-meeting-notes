using Luval.GPT.Agent.Core;
using Luval.GPT.Agent.Core.Activity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.MN.Core.Activities
{
    public class FindAudioFilesActivity : BaseActivity
    {
        public FindAudioFilesActivity(ILogger logger) : base(logger)
        {
            Name = "Find Audio Files";
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
            var workingDir = InputParameters["WorkingDirectory"];
            if (string.IsNullOrEmpty(workingDir)) workingDir = Environment.CurrentDirectory;
            var dir = new DirectoryInfo(workingDir);
            if (!dir.Exists) throw new ArgumentException($"Working directory {workingDir} does not exist");
            var files = dir.GetFiles("*.*", SearchOption.TopDirectoryOnly)
                .Where(i => i.Extension.ToLower() == ".mp3" ||
                            i.Extension.ToLower() == ".wav" ||
                            i.Extension.ToLower() == ".wma" ||
                            i.Extension.ToLower() == ".m4a");
            foreach (var file in files)
            {
                Result[file.Name] = file.FullName;
            }
        }
    }
}
