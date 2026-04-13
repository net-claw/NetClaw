using System.Text.Json.Serialization;

namespace NetClaw.AspNetCore.Extensions.ErrorHandlers
{
    public class ProblemResult
    {
        public ProblemResult()
        {
        }

        public ProblemResult(string errorMessage)
          : this((string)null, errorMessage)
        {
        }

        public ProblemResult(string errorCode, string errorMessage)
        {
            this.ErrorCode = errorCode;
            this.ErrorMessage = errorMessage;
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string ErrorCode { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public IDictionary<string, object> References { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string ErrorMessage { get; set; }
    }
}
