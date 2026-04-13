using System.Net;
using System.Text.Json.Serialization;

namespace NetClaw.AspNetCore.Extensions.ErrorHandlers
{
    public class ProblemDetails
    {
        private HttpStatusCode _status;

        public ProblemDetails(string errorMessage = null)
        {
            this.Status = HttpStatusCode.BadRequest;
            this.ErrorMessage = errorMessage;
        }

        [JsonConverter(typeof(StatusCodeEnumJsonConverter))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public HttpStatusCode Status
        {
            get => this._status;
            set
            {
                this._status = value;
                this.ErrorCode = this._status.ToString();
            }
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string TraceId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string ErrorCode { get; private set; }

        public string ErrorMessage { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public ProblemResultCollection ErrorDetails { get; set; }
    }
}
