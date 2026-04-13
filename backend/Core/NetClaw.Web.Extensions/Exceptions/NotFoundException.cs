using System.Runtime.Serialization;

namespace NetClaw.AspNetCore.Extensions.Exceptions
{
    [Serializable()]
    public class NotFoundException : Exception
    {
        public Error[] Errors { get; }
        public NotFoundException()
        {
        }

        public NotFoundException(string message) : base(message)
        {
        }

        public NotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public NotFoundException(params Error[] value) => Errors = value;

        public NotFoundException(string code, string message) => Errors = new[] { new Error { Code = code, Message = message } };
    }
}
