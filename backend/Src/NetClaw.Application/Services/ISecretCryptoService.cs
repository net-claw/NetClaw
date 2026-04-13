namespace NetClaw.Application.Services;

public interface ISecretCryptoService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}
