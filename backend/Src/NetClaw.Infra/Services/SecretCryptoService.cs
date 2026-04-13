using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NetClaw.Application.Options;
using NetClaw.Application.Services;

namespace NetClaw.Infra.Services;

internal sealed class SecretCryptoService(
    IDataProtectionProvider dataProtectionProvider,
    IOptions<AppConfigOptions> options) : ISecretCryptoService
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(options.Value.ProtectorKey);

    public string Encrypt(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        return _protector.Protect(plaintext.Trim());
    }

    public string Decrypt(string ciphertext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ciphertext);
        return _protector.Unprotect(ciphertext.Trim());
    }
}
