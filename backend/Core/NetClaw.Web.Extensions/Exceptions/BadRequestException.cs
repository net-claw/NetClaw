namespace NetClaw.AspNetCore.Extensions.Exceptions
{
    [Serializable()]
    public class BadRequestException : Exception
    {
        public Error[] Errors { get; }

        public BadRequestException()
        {
        }

        public BadRequestException(params Error[] value) => Errors = value;

        public BadRequestException(string code, string message) => Errors = new[] { new Error { Code = code, Message = message } };
    }
}
