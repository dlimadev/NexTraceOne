using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

using Microsoft.Extensions.Logging;

using NexTraceOne.IdentityAccess.Application.Abstractions;

namespace NexTraceOne.IdentityAccess.Infrastructure.Services;

/// <summary>
/// Implementação de ISamlService usando apenas BCL .NET.
/// Constrói AuthnRequests SAML 2.0 (Redirect Binding) e valida SAMLResponses (POST Binding).
/// Não depende de NuGet packages de terceiros para SAML.
/// </summary>
internal sealed class SamlService(ILogger<SamlService> logger) : ISamlService
{
    private const string SamlAssertionNs = "urn:oasis:names:tc:SAML:2.0:assertion";
    private const string SamlProtocolNs = "urn:oasis:names:tc:SAML:2.0:protocol";

    /// <inheritdoc />
    public string BuildAuthnRequestUrl(
        string ssoUrl,
        string spEntityId,
        string acsUrl,
        string requestId,
        string relayState)
    {
        var issuedAt = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        var xml = $"""
            <samlp:AuthnRequest
                xmlns:samlp="{SamlProtocolNs}"
                xmlns:saml="{SamlAssertionNs}"
                ID="{requestId}"
                Version="2.0"
                IssueInstant="{issuedAt}"
                Destination="{EscapeXml(ssoUrl)}"
                AssertionConsumerServiceURL="{EscapeXml(acsUrl)}"
                ProtocolBinding="urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST">
                <saml:Issuer>{EscapeXml(spEntityId)}</saml:Issuer>
            </samlp:AuthnRequest>
            """;

        // Deflate (raw), base64, URL-encode
        var xmlBytes = Encoding.UTF8.GetBytes(xml);
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(xmlBytes, 0, xmlBytes.Length);
        }

        var deflated = output.ToArray();
        var base64 = Convert.ToBase64String(deflated);
        var encoded = Uri.EscapeDataString(base64);
        var encodedRelayState = Uri.EscapeDataString(relayState);

        var separator = ssoUrl.Contains('?') ? "&" : "?";
        return $"{ssoUrl}{separator}SAMLRequest={encoded}&RelayState={encodedRelayState}";
    }

    /// <inheritdoc />
    public SamlParsedAssertion ParseSamlResponse(string samlResponseBase64, string idpCertificatePem)
    {
        var xmlBytes = Convert.FromBase64String(samlResponseBase64);
        var xmlString = Encoding.UTF8.GetString(xmlBytes);

        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(xmlString);

        ValidateSignature(doc, idpCertificatePem);

        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("saml", SamlAssertionNs);
        nsMgr.AddNamespace("samlp", SamlProtocolNs);

        var nameId = doc.SelectSingleNode("//saml:NameID", nsMgr)?.InnerText
            ?? throw new InvalidOperationException("SAMLResponse missing saml:NameID element.");

        var email = FindAttribute(doc, nsMgr, "email", "EmailAddress")
            ?? nameId;

        var name = FindAttribute(doc, nsMgr, "cn", "displayName", "name");

        var groups = FindMultiValueAttribute(doc, nsMgr, "groups", "memberOf");

        logger.LogDebug(
            "SAML assertion parsed: NameId={NameId} Email={Email} Groups={GroupCount}",
            nameId, email, groups.Count);

        return new SamlParsedAssertion(nameId, email, name, groups);
    }

    /// <summary>
    /// Valida a assinatura XML da SAMLResponse usando o certificado PEM do IdP.
    /// Suporta tanto assinatura na Response como na Assertion.
    /// </summary>
    private static void ValidateSignature(XmlDocument doc, string idpCertificatePem)
    {
        // Normaliza o PEM: remove headers e whitespace
        var pemContent = idpCertificatePem
            .Replace("-----BEGIN CERTIFICATE-----", string.Empty)
            .Replace("-----END CERTIFICATE-----", string.Empty)
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty)
            .Trim();

        var certBytes = Convert.FromBase64String(pemContent);
        using var cert = X509CertificateLoader.LoadCertificate(certBytes);

        var signedXml = new SignedXml(doc);
        var signatureNode = doc.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl);

        if (signatureNode.Count == 0)
        {
            // Aceitar responses não assinadas apenas em modo de desenvolvimento — em produção rejeitar.
            throw new InvalidOperationException("SAMLResponse does not contain an XML digital signature.");
        }

        signedXml.LoadXml((XmlElement)signatureNode[0]!);

        var rsaKey = cert.GetRSAPublicKey()
            ?? throw new InvalidOperationException("IdP certificate does not contain an RSA public key.");

        if (!signedXml.CheckSignature(rsaKey))
        {
            throw new CryptographicException("SAMLResponse signature validation failed. The response may have been tampered with.");
        }
    }

    /// <summary>
    /// Procura o valor de um atributo SAML por nome (primeiro match).
    /// Suporta múltiplos nomes candidatos (ex: "email" ou "EmailAddress").
    /// </summary>
    private static string? FindAttribute(XmlDocument doc, XmlNamespaceManager nsMgr, params string[] attributeNames)
    {
        foreach (var attrName in attributeNames)
        {
            var node = doc.SelectSingleNode(
                $"//saml:Attribute[@Name='{attrName}']/saml:AttributeValue", nsMgr);
            if (node is not null)
            {
                return node.InnerText;
            }
        }

        return null;
    }

    /// <summary>
    /// Extrai todos os valores de um atributo SAML multi-valor (ex: grupos).
    /// Suporta múltiplos nomes candidatos.
    /// </summary>
    private static IReadOnlyList<string> FindMultiValueAttribute(
        XmlDocument doc,
        XmlNamespaceManager nsMgr,
        params string[] attributeNames)
    {
        foreach (var attrName in attributeNames)
        {
            var nodes = doc.SelectNodes(
                $"//saml:Attribute[@Name='{attrName}']/saml:AttributeValue", nsMgr);

            if (nodes is { Count: > 0 })
            {
                var values = new List<string>(nodes.Count);
                foreach (XmlNode node in nodes)
                {
                    if (!string.IsNullOrWhiteSpace(node.InnerText))
                    {
                        values.Add(node.InnerText);
                    }
                }

                if (values.Count > 0)
                {
                    return values;
                }
            }
        }

        return Array.Empty<string>();
    }

    /// <summary>Escapa caracteres especiais XML para uso em atributos.</summary>
    private static string EscapeXml(string value) =>
        value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
}
