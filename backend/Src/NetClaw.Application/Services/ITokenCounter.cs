using Microsoft.Extensions.AI;

namespace NetClaw.Application.Services;

public interface ITokenCounter
{
    int Count(string text);
    int Count(IEnumerable<ChatMessage> messages);
}
