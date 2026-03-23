using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Shuttle.ServiceDefaults.DataProtection;

namespace Shuttle.Core.DataProtection;

public static class ShuttleDataTaxonomy {
    public static string Name => nameof(ShuttleDataTaxonomy);

    public static DataClassification Obscured => new(Name, nameof(Obscured));
    public static DataClassification Pii => new(Name, nameof(Pii));

    public static IRedactionBuilder AddShuttleRedaction(IRedactionBuilder builder) {
        builder.SetRedactor<ObscuringHashRedactor>(Obscured);
        return builder;
    }
}
