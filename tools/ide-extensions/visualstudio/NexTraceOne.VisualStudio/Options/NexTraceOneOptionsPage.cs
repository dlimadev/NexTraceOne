using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;

namespace NexTraceOne.VisualStudio;

/// <summary>
/// Página de opções da extensão NexTraceOne.
/// Acessível via Tools → Options → NexTraceOne → General.
/// Permite configurar a URL do servidor e a chave de API sem necessidade de redeploy.
/// </summary>
public sealed class NexTraceOneOptionsPage : DialogPage
{
    private string _serverUrl = "http://localhost:5000";
    private string _apiKey = string.Empty;
    private string _persona = "Engineer";
    private string _defaultEnvironment = "production";

    [Category("Connection")]
    [DisplayName("Server URL")]
    [Description("Base URL of the NexTraceOne server (e.g. https://nex.company.internal).")]
    public string ServerUrl
    {
        get => _serverUrl;
        set => _serverUrl = string.IsNullOrWhiteSpace(value) ? "http://localhost:5000" : value.TrimEnd('/');
    }

    [Category("Connection")]
    [DisplayName("API Key")]
    [Description("NexTraceOne IDE API key. Stored in VS user settings (not committed to source control).")]
    [PasswordPropertyText(true)]
    public string ApiKey
    {
        get => _apiKey;
        set => _apiKey = value ?? string.Empty;
    }

    [Category("Behaviour")]
    [DisplayName("Persona")]
    [Description("Your persona in NexTraceOne. Affects the level of detail and focus of AI responses.")]
    [TypeConverter(typeof(PersonaTypeConverter))]
    public string Persona
    {
        get => _persona;
        set => _persona = string.IsNullOrWhiteSpace(value) ? "Engineer" : value;
    }

    [Category("Behaviour")]
    [DisplayName("Default Environment")]
    [Description("Default environment context for queries (e.g. production, staging, development).")]
    public string DefaultEnvironment
    {
        get => _defaultEnvironment;
        set => _defaultEnvironment = string.IsNullOrWhiteSpace(value) ? "production" : value;
    }
}

/// <summary>Conversor para lista de personas válidas na combo box de opções.</summary>
internal sealed class PersonaTypeConverter : StringConverter
{
    private static readonly string[] ValidPersonas =
    [
        "Engineer", "TechLead", "Architect", "Product", "Executive", "PlatformAdmin", "Auditor"
    ];

    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => true;

    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
        => new(ValidPersonas);
}
