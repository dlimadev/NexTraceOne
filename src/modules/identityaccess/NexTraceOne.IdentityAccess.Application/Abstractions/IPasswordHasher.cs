namespace NexTraceOne.Identity.Application.Abstractions;

/// <summary>
/// Serviço responsável por hash e verificação de senhas locais.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Gera o hash de uma senha em texto plano.</summary>
    string Hash(string password);

    /// <summary>Verifica se a senha em texto plano corresponde ao hash persistido.</summary>
    bool Verify(string password, string hash);
}
