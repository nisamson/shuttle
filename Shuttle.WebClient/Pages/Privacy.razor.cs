using System.Collections.Frozen;
using MudBlazor;

namespace Shuttle.WebClient.Pages;

public partial class Privacy {
    
    private MudPageContentNavigation contents = null!;
    
    private const string IntroId = "intro";
    private const string WhatICollectId = "what-i-collect";
    private const string HowIUseItId = "how-i-use-it";
    private const string HowIProtectItId = "how-i-protect-it";
    private const string ContactId = "contact";

    private static readonly IReadOnlyList<MudPageContentSection> Sections = [
        new("Introduction", IntroId),
        new("What I Collect", WhatICollectId),
        new("How I Use It", HowIUseItId),
        new("How I Protect It", HowIProtectItId),
        new("Contact", ContactId)
    ];

    private static readonly FrozenDictionary<string, int> ClassLevels = new Dictionary<string, int> {
        ["h1"] = 0,
        ["h2"] = 1,
        ["h3"] = 2,
        ["h4"] = 3,
        ["h5"] = 4,
        ["h6"] = 5
    }.ToFrozenDictionary();

    protected override void OnAfterRender(bool firstRender) {
        if (firstRender) {
            foreach (var section in Sections) {
                contents.AddSection(section, false);
            }

            StateHasChanged();
        }
    }

}