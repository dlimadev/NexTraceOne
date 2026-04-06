using System.Text.Json;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Pdf;

namespace NexTraceOne.AuditCompliance.Infrastructure.Services;

/// <summary>
/// Renderiza relatórios de auditoria no formato PDF usando PdfSharpCore (MIT license).
///
/// O layout segue o padrão enterprise do NexTraceOne com cabeçalho, corpo tabular e rodapé.
/// PdfSharpCore é uma biblioteca open-source com licença MIT, permitindo uso comercial
/// sem restrições — segura para distribuição no NexTraceOne.
///
/// Persona: Auditor, Executive.
/// </summary>
public sealed class PdfReportRenderer(IDateTimeProvider dateTimeProvider) : IReportRenderer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // ── Layout constants ──
    private const double PageMarginH = 40;
    private const double PageMarginV = 30;
    private const double HeaderHeight = 40;
    private const double FooterHeight = 30;
    private const double LineSpacing = 14;
    private const double SectionSpacing = 20;
    private const double IndentStep = 16;
    private const double KeyColumnWidth = 200;

    // ── Fonts ──
    private static readonly XFont TitleFont = new("Arial", 16, XFontStyle.Bold);
    private static readonly XFont TimestampFont = new("Arial", 8, XFontStyle.Regular);
    private static readonly XFont SectionFont = new("Arial", 10, XFontStyle.Bold);
    private static readonly XFont KeyFont = new("Arial", 9, XFontStyle.Bold);
    private static readonly XFont ValueFont = new("Arial", 9, XFontStyle.Regular);
    private static readonly XFont IndexFont = new("Arial", 8, XFontStyle.Regular);
    private static readonly XFont FooterFont = new("Arial", 8, XFontStyle.Regular);

    // ── Colors ──
    private static readonly XColor TitleColor = XColor.FromArgb(0x1A, 0x56, 0xDB);
    private static readonly XColor SectionColor = XColor.FromArgb(0x2B, 0x6C, 0xB0);
    private static readonly XColor KeyColor = XColor.FromArgb(0x4A, 0x4A, 0x4A);
    private static readonly XColor ValueColor = XColor.FromArgb(0x33, 0x33, 0x33);
    private static readonly XColor HeaderLineColor = XColor.FromArgb(0xCC, 0xCC, 0xCC);
    private static readonly XColor IndexColor = XColor.FromArgb(0x99, 0x99, 0x99);
    private static readonly XColor FooterColor = XColor.FromArgb(0x88, 0x88, 0x88);

    /// <inheritdoc />
    public Task<RenderedReport> RenderAsync(
        object report,
        string format,
        CancellationToken cancellationToken = default)
    {
        var jsonElement = JsonSerializer.SerializeToElement(report, SerializerOptions);
        var now = dateTimeProvider.UtcNow;
        var timestamp = now.ToString("yyyy-MM-dd HH:mm:ss UTC");

        using var document = new PdfDocument();
        document.Info.Title = "NexTraceOne — Audit Report";
        document.Info.Author = "NexTraceOne Audit Module";
        document.Info.Subject = $"Audit Report generated at {timestamp}";

        var ctx = new RenderContext(document, timestamp);
        ctx.EnsurePage();

        // ── Render content ──
        RenderJsonElement(ctx, jsonElement, depth: 0);

        // ── Number all pages (footer) ──
        var totalPages = document.PageCount;
        for (var i = 0; i < totalPages; i++)
        {
            DrawFooter(ctx, document.Pages[i], i + 1, totalPages);
        }

        using var stream = new MemoryStream();
        document.Save(stream, false);
        var bytes = stream.ToArray();

        return Task.FromResult(new RenderedReport(
            bytes,
            "application/pdf",
            $"audit-report-{now:yyyyMMdd-HHmmss}.pdf"));
    }

    /// <summary>
    /// Contexto de renderização que mantém a posição Y corrente e cria novas páginas conforme necessário.
    /// </summary>
    private sealed class RenderContext
    {
        public PdfDocument Document { get; }
        public string Timestamp { get; }
        public PdfPage? CurrentPage { get; private set; }
        public XGraphics? Gfx { get; private set; }
        public double Y { get; set; }
        public double ContentBottom { get; private set; }
        private bool _headerDrawn;

        public RenderContext(PdfDocument document, string timestamp)
        {
            Document = document;
            Timestamp = timestamp;
        }

        public double ContentWidth => (CurrentPage?.Width.Point ?? 595) - 2 * PageMarginH;

        public void EnsurePage()
        {
            if (CurrentPage != null && Y < ContentBottom)
                return;

            Gfx?.Dispose();
            CurrentPage = Document.AddPage();
            CurrentPage.Size = PdfSharpCore.PageSize.A4;
            Gfx = XGraphics.FromPdfPage(CurrentPage);
            Y = PageMarginV;
            ContentBottom = CurrentPage.Height.Point - PageMarginV - FooterHeight;
            _headerDrawn = false;
        }

        public void DrawHeader()
        {
            if (_headerDrawn || Gfx is null || CurrentPage is null) return;
            _headerDrawn = true;

            var pageWidth = CurrentPage.Width.Point;

            // Title
            Gfx.DrawString(
                "NexTraceOne — Audit Report",
                TitleFont,
                new XSolidBrush(TitleColor),
                new XRect(PageMarginH, Y, pageWidth - 2 * PageMarginH, HeaderHeight),
                XStringFormats.TopLeft);

            // Timestamp (right-aligned)
            Gfx.DrawString(
                Timestamp,
                TimestampFont,
                new XSolidBrush(FooterColor),
                new XRect(PageMarginH, Y + 4, pageWidth - 2 * PageMarginH, HeaderHeight),
                XStringFormats.TopRight);

            Y += HeaderHeight;

            // Separator line
            Gfx.DrawLine(
                new XPen(HeaderLineColor, 0.5),
                PageMarginH, Y,
                pageWidth - PageMarginH, Y);

            Y += 8;
        }

        public void EnsureSpace(double needed)
        {
            if (Y + needed > ContentBottom)
            {
                EnsurePage();
                DrawHeader();
            }
        }
    }

    /// <summary>
    /// Renderiza recursivamente um JsonElement como conteúdo PDF estruturado.
    /// </summary>
    private static void RenderJsonElement(RenderContext ctx, JsonElement element, int depth)
    {
        ctx.DrawHeader();

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    {
                        ctx.EnsureSpace(SectionSpacing + LineSpacing);
                        ctx.Y += depth > 0 ? 6 : SectionSpacing * 0.5;

                        ctx.Gfx!.DrawString(
                            FormatKey(property.Name),
                            SectionFont,
                            new XSolidBrush(SectionColor),
                            new XPoint(PageMarginH + depth * IndentStep, ctx.Y));
                        ctx.Y += LineSpacing + 2;

                        RenderJsonElement(ctx, property.Value, depth + 1);
                    }
                    else
                    {
                        ctx.EnsureSpace(LineSpacing);

                        var x = PageMarginH + depth * IndentStep;

                        // Key
                        ctx.Gfx!.DrawString(
                            FormatKey(property.Name),
                            KeyFont,
                            new XSolidBrush(KeyColor),
                            new XRect(x, ctx.Y, KeyColumnWidth, LineSpacing),
                            XStringFormats.TopLeft);

                        // Value
                        var valueX = x + KeyColumnWidth;
                        var valueWidth = ctx.ContentWidth - (depth * IndentStep) - KeyColumnWidth;
                        if (valueWidth < 50) valueWidth = 50;

                        var valueText = FormatValue(property.Value);
                        var tf = new XTextFormatter(ctx.Gfx);
                        var valueRect = new XRect(valueX, ctx.Y, valueWidth, LineSpacing * 3);
                        tf.DrawString(valueText, ValueFont, new XSolidBrush(ValueColor), valueRect);

                        ctx.Y += LineSpacing;
                    }
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    ctx.EnsureSpace(LineSpacing + 4);
                    ctx.Y += 4;

                    ctx.Gfx!.DrawString(
                        $"[{index}]",
                        IndexFont,
                        new XSolidBrush(IndexColor),
                        new XPoint(PageMarginH + depth * IndentStep, ctx.Y));
                    ctx.Y += LineSpacing;

                    RenderJsonElement(ctx, item, depth + 1);
                    index++;
                }
                break;

            default:
                ctx.EnsureSpace(LineSpacing);
                ctx.Gfx!.DrawString(
                    FormatValue(element),
                    ValueFont,
                    new XSolidBrush(ValueColor),
                    new XPoint(PageMarginH + depth * IndentStep, ctx.Y));
                ctx.Y += LineSpacing;
                break;
        }
    }

    private static void DrawFooter(RenderContext ctx, PdfPage page, int pageNumber, int totalPages)
    {
        using var gfx = XGraphics.FromPdfPage(page);
        var footerText = $"Page {pageNumber} of {totalPages}  |  Generated by NexTraceOne Audit Module";
        var pageWidth = page.Width.Point;
        var footerY = page.Height.Point - PageMarginV;

        gfx.DrawString(
            footerText,
            FooterFont,
            new XSolidBrush(FooterColor),
            new XRect(PageMarginH, footerY, pageWidth - 2 * PageMarginH, FooterHeight),
            XStringFormats.TopCenter);
    }

    private static string FormatKey(string key)
    {
        // camelCase → Title Case
        if (string.IsNullOrEmpty(key)) return key;
        var result = new System.Text.StringBuilder();
        result.Append(char.ToUpper(key[0], System.Globalization.CultureInfo.InvariantCulture));
        for (var i = 1; i < key.Length; i++)
        {
            if (char.IsUpper(key[i]) && !char.IsUpper(key[i - 1]))
                result.Append(' ');
            result.Append(key[i]);
        }
        return result.ToString();
    }

    private static string FormatValue(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString() ?? "",
        JsonValueKind.Number => el.GetRawText(),
        JsonValueKind.True => "Yes",
        JsonValueKind.False => "No",
        JsonValueKind.Null => "—",
        _ => el.GetRawText()
    };
}
