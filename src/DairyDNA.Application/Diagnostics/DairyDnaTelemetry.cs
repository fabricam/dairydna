using System.Diagnostics;

namespace DairyDNA.Application.Diagnostics;

/// <summary>
/// Thin <see cref="ActivitySource"/> wrapper so the demo/generate/optimize jobs show up as spans in
/// the Aspire dashboard (spec 013 FR-004) without adding a new telemetry stack. The source name
/// intentionally matches the API host's <c>IHostEnvironment.ApplicationName</c> ("DairyDNA.Api"),
/// which <c>DairyDNA.ServiceDefaults</c> already registers via
/// <c>tracing.AddSource(builder.Environment.ApplicationName)</c> — no ServiceDefaults changes needed.
/// </summary>
public static class DairyDnaTelemetry
{
    public const string ActivitySourceName = "DairyDNA.Api";

    public static readonly ActivitySource Source = new(ActivitySourceName);
}
