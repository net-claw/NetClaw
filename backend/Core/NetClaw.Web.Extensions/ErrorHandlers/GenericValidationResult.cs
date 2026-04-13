using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace NetClaw.AspNetCore.Extensions.ErrorHandlers
{
    public class GenericValidationResult
    {
        public GenericValidationResult(ValidationResult validationResult)
          : this(validationResult.ErrorMessage, validationResult.MemberNames.ToArray<string>())
        {
        }

        public GenericValidationResult(string errorCode, string errorMessage)
          : this(errorCode, errorMessage, (string[])null)
        {
        }

        public GenericValidationResult(string errorMessage, string[] memberNames)
          : this((string)null, errorMessage, memberNames)
        {
        }

        public GenericValidationResult(string errorCode, string errorMessage, [AllowNull] string[] memberNames)
        {
            this.ErrorCode = errorCode;
            this.ErrorMessage = errorMessage;
            this.MemberNames = memberNames == null || !((IEnumerable<string>)memberNames).Any<string>() ? (IEnumerable<string>)(string[])null : (IEnumerable<string>)memberNames;
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string ErrorCode { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public IDictionary<string, object> References { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public IEnumerable<string> MemberNames { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string ErrorMessage { get; set; }

        public override string ToString() => this.ErrorMessage ?? base.ToString();
    }
}
