namespace NetClaw.AspNetCore.Extensions.ErrorHandlers
{
    public static class ProblemDetailsExtensions
    {
        public static ProblemResult ToProblemResult(this GenericValidationResult result) => new ProblemResult()
        {
            References = result.References,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage
        };
    }
}
