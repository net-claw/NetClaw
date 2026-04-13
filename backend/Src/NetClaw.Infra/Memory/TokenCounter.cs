using Microsoft.Extensions.AI;
using NetClaw.Application.Models.Llm;
using NetClaw.Application.Services;
using TiktokenSharp;

namespace NetClaw.Infra.Memory;

public sealed class TokenCounter : ITokenCounter
{
    // Overhead per message (role + structural tokens), matches OpenAI's formula
    private const int PerMessageOverhead = 4;

    private readonly TikToken _encoding;

    public TokenCounter(ContextSettings settings)
    {
        _encoding = TikToken.GetEncoding(settings.TiktokenEncoding);
    }

    public int Count(string text) =>
        string.IsNullOrEmpty(text) ? 0 : _encoding.Encode(text).Count;

    public int Count(IEnumerable<ChatMessage> messages) =>
        messages.Sum(m => Count(m.Text ?? string.Empty) + PerMessageOverhead);
}
