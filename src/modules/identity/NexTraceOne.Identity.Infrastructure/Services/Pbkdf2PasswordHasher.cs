using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.ValueObjects;

namespace NexTraceOne.Identity.Infrastructure.Services;

/// <summary>
/// Implementação de hash de senha usando PBKDF2 com SHA-256.
/// Delega ao Value Object HashedPassword do domínio, que encapsula
/// a lógica de derivação com salt aleatório de 16 bytes e 100.000 iterações.
/// </summary>
internal sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    /// <inheritdoc />
    public string Hash(string password) => HashedPassword.FromPlainText(password).Value;

    /// <inheritdoc />
    public bool Verify(string password, string hash) => HashedPassword.FromHash(hash).Verify(password);
}
