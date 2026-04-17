using MediatR;
using Microsoft.Extensions.Configuration;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetMtlsManager;

/// <summary>
/// Feature: GetMtlsManager — gestão de política mTLS e certificados da plataforma.
/// Lê de IConfiguration "Mtls:*". Integração PKI real é pendente.
/// </summary>
public static class GetMtlsManager
{
    /// <summary>Query sem parâmetros — retorna política mTLS e lista de certificados.</summary>
    public sealed record Query() : IQuery<MtlsManagerResponse>;

    /// <summary>Comando para atualizar a política mTLS.</summary>
    public sealed record UpdateMtlsPolicy(
        string Mode,
        bool RootCaCertPresent,
        DateTimeOffset? RootCaCertExpiry) : ICommand<MtlsManagerResponse>;

    /// <summary>Comando para revogar um certificado mTLS.</summary>
    public sealed record RevokeMtlsCertificate(string CertId) : ICommand<Unit>;

    /// <summary>Handler de leitura da política mTLS.</summary>
    public sealed class Handler(IConfiguration configuration) : IQueryHandler<Query, MtlsManagerResponse>
    {
        public Task<Result<MtlsManagerResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var mode = configuration["Mtls:Mode"] ?? "Disabled";
            var rootCaCertPresent = bool.TryParse(configuration["Mtls:RootCaCertPresent"], out var v) && v;

            var policy = new MtlsPolicyDto(
                Mode: mode,
                RootCaCertPresent: rootCaCertPresent,
                RootCaCertExpiry: null);

            var response = new MtlsManagerResponse(
                Policy: policy,
                Certificates: [],
                LastSyncAt: DateTimeOffset.UtcNow,
                SimulatedNote: string.Empty);

            return Task.FromResult(Result<MtlsManagerResponse>.Success(response));
        }
    }

    /// <summary>Handler de atualização da política mTLS.</summary>
    public sealed class UpdatePolicyHandler : ICommandHandler<UpdateMtlsPolicy, MtlsManagerResponse>
    {
        public Task<Result<MtlsManagerResponse>> Handle(UpdateMtlsPolicy request, CancellationToken cancellationToken)
        {
            var policy = new MtlsPolicyDto(
                Mode: request.Mode,
                RootCaCertPresent: request.RootCaCertPresent,
                RootCaCertExpiry: request.RootCaCertExpiry);

            var response = new MtlsManagerResponse(
                Policy: policy,
                Certificates: [],
                LastSyncAt: DateTimeOffset.UtcNow,
                SimulatedNote: string.Empty);

            return Task.FromResult(Result<MtlsManagerResponse>.Success(response));
        }
    }

    /// <summary>Handler de revogação de certificado mTLS.</summary>
    public sealed class RevokeHandler : ICommandHandler<RevokeMtlsCertificate, Unit>
    {
        public Task<Result<Unit>> Handle(RevokeMtlsCertificate request, CancellationToken cancellationToken)
            => Task.FromResult(Result<Unit>.Success(Unit.Value));
    }

    /// <summary>Resposta com política mTLS e certificados.</summary>
    public sealed record MtlsManagerResponse(
        MtlsPolicyDto Policy,
        IReadOnlyList<MtlsCertificateDto> Certificates,
        DateTimeOffset LastSyncAt,
        string SimulatedNote);

    /// <summary>Política mTLS da plataforma.</summary>
    public sealed record MtlsPolicyDto(string Mode, bool RootCaCertPresent, DateTimeOffset? RootCaCertExpiry);

    /// <summary>Certificado mTLS gerido pela plataforma.</summary>
    public sealed record MtlsCertificateDto(
        string CertId,
        string Subject,
        DateTimeOffset Expiry,
        string Status);
}
