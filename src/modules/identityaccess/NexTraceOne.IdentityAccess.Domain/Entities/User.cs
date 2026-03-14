using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Identity.Domain.Events;
using NexTraceOne.Identity.Domain.ValueObjects;

namespace NexTraceOne.Identity.Domain.Entities;

/// <summary>
/// Aggregate Root que representa um usuário autenticável da plataforma.
/// Mantém credenciais locais, vínculo federado e estado de bloqueio de login.
/// </summary>
public sealed class User : AggregateRoot<UserId>
{
    private User() { }

    /// <summary>Email normalizado do usuário.</summary>
    public Email Email { get; private set; } = null!;

    /// <summary>Nome completo do usuário.</summary>
    public FullName FullName { get; private set; } = null!;

    /// <summary>Hash BCrypt da senha local, quando existir.</summary>
    public HashedPassword? PasswordHash { get; private set; }

    /// <summary>Indica se o usuário está ativo.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Data/hora UTC do último login bem-sucedido.</summary>
    public DateTimeOffset? LastLoginAt { get; private set; }

    /// <summary>Total de tentativas falhas consecutivas.</summary>
    public int FailedLoginAttempts { get; private set; }

    /// <summary>Fim do período de bloqueio por tentativas inválidas.</summary>
    public DateTimeOffset? LockoutEnd { get; private set; }

    /// <summary>Provider federado associado ao usuário, quando houver.</summary>
    public string? FederationProvider { get; private set; }

    /// <summary>Identificador externo do provider federado.</summary>
    public string? ExternalId { get; private set; }

    /// <summary>Cria um usuário local com senha armazenada em BCrypt.</summary>
    public static User CreateLocal(Email email, FullName fullName, HashedPassword passwordHash)
    {
        var user = new User
        {
            Id = UserId.New(),
            Email = Guard.Against.Null(email),
            FullName = Guard.Against.Null(fullName),
            PasswordHash = Guard.Against.Null(passwordHash),
            IsActive = true
        };

        user.RaiseDomainEvent(new UserCreatedDomainEvent(user.Id, user.Email.Value));
        return user;
    }

    /// <summary>Cria um usuário federado sem senha local.</summary>
    public static User CreateFederated(Email email, FullName fullName, string provider, string externalId)
    {
        var user = new User
        {
            Id = UserId.New(),
            Email = Guard.Against.Null(email),
            FullName = Guard.Against.Null(fullName),
            IsActive = true,
            FederationProvider = Guard.Against.NullOrWhiteSpace(provider),
            ExternalId = Guard.Against.NullOrWhiteSpace(externalId)
        };

        user.RaiseDomainEvent(new UserCreatedDomainEvent(user.Id, user.Email.Value));
        return user;
    }

    /// <summary>Vincula ou atualiza a identidade federada do usuário.</summary>
    public void LinkFederatedIdentity(string provider, string externalId)
    {
        FederationProvider = Guard.Against.NullOrWhiteSpace(provider);
        ExternalId = Guard.Against.NullOrWhiteSpace(externalId);
    }

    /// <summary>Registra um login bem-sucedido e limpa o bloqueio.</summary>
    public void RegisterSuccessfulLogin(DateTimeOffset occurredAt)
    {
        LastLoginAt = occurredAt;
        FailedLoginAttempts = 0;
        LockoutEnd = null;
    }

    /// <summary>Registra uma tentativa de login inválida e bloqueia após o limite.</summary>
    public void RegisterFailedLogin(DateTimeOffset occurredAt, int maxAttempts = 5, TimeSpan? lockoutDuration = null)
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts < maxAttempts)
        {
            return;
        }

        LockoutEnd = occurredAt.Add(lockoutDuration ?? TimeSpan.FromMinutes(15));
        FailedLoginAttempts = 0;
        RaiseDomainEvent(new UserLockedDomainEvent(Id, LockoutEnd.Value));
    }

    /// <summary>Define ou substitui a senha local do usuário.</summary>
    public void SetPassword(HashedPassword passwordHash)
        => PasswordHash = Guard.Against.Null(passwordHash);

    /// <summary>Atualiza o nome completo do usuário.</summary>
    public void UpdateProfile(FullName fullName)
        => FullName = Guard.Against.Null(fullName);

    /// <summary>Desativa o usuário para impedir novos logins.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Reativa um usuário previamente desativado.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Indica se o usuário está bloqueado na data informada.</summary>
    public bool IsLocked(DateTimeOffset now)
        => LockoutEnd.HasValue && LockoutEnd.Value > now;
}

/// <summary>Identificador fortemente tipado de User.</summary>
public sealed record UserId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static UserId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static UserId From(Guid id) => new(id);
}
