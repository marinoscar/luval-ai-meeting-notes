using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.MN.Core
{
    public class AgentTask
    {
        public AgentTask()
        {
            Id = Guid.NewGuid().ToString();
            UtcCreatedOn = DateTime.UtcNow;
            UtcUpdatedOn = UtcCreatedOn;
            RetryCount = 0;
            Status = AgentTaskStatus.Pending;
            CreatedByAgent = GetAgent();
            UpdatedByAgent = CreatedByAgent;
        }
        public string Id { get; set; }
        public string? Name { get; set; }
        public string? Data { get; set; }
        public int RetryCount { get; set; }
        public string UpdatedByAgent { get; set; }
        public string CreatedByAgent { get; set; }
        public AgentTaskStatus Status { get; set; }
        public DateTime UtcCreatedOn { get; set; }
        public DateTime UtcUpdatedOn { get; set; }

        public static string GetAgent()
        {
            return $"{Environment.MachineName} - {Environment.UserDomainName}";
        }

    }

    public enum AgentTaskStatus { None, Pending, InProgress, Success, Failure }
}
