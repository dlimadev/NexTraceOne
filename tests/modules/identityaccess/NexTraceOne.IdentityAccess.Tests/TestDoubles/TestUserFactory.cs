using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Tests.TestDoubles;

/// <summary>
/// Fábrica de utilizadores de teste para os testes unitários do módulo Identity.
/// Centraliza a criação de utilizadores com configurações específicas para evitar
/// duplicação nos testes.
/// </summary>
internal static class TestUserFactory
{
    /// <summary>
    /// Cria um utilizador local com MFA TOTP habilitado.
    /// O segredo base32 "JBSWY3DPEHPK3PXP" é um segredo de teste padrão RFC 4648.
    /// </summary>
    public static User CreateMfaUser()
    {
        var user = User.CreateLocal(
            Email.Create("mfa-user@example.com"),
            FullName.Create("MFA", "User"),
            HashedPassword.FromPlainText("P@ssw0rd123"));

        user.EnableMfa("TOTP", "JBSWY3DPEHPK3PXP");
        return user;
    }

    /// <summary>
    /// Cria um utilizador local sem MFA habilitado.
    /// </summary>
    public static User CreateRegularUser()
        => User.CreateLocal(
            Email.Create("user@example.com"),
            FullName.Create("Regular", "User"),
            HashedPassword.FromPlainText("P@ssw0rd123"));
}
