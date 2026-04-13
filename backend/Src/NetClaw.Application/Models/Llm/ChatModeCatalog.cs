namespace NetClaw.Application.Models.Llm;

public sealed class ChatModeCatalog
{
    private const string SandboxInstructions =
        "You have a sandbox_exec tool to run shell commands in a Docker sandbox container. " +
        "Use it for any task requiring CLI tools, code execution, automation, or file generation. " +
        "Files written to /workspace/downloads/ are downloadable via /api/v1/sandbox/downloads/<filename>.";

    public string GetInstructions() =>
        "You are a helpful assistant. Use tools when they can help. " + SandboxInstructions;
}
