using System.Collections.Generic;
using Microsoft.FluentUI.AspNetCore.Components;
using Shuttle.WebClient.Models;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Shuttle.WebClient.Pages;

public partial class Home
{
    private sealed record FeatureCard(string Title, string Description, string Href, Icon Icon);

    private static readonly IReadOnlyList<FeatureCard> Features =
    [
        new FeatureCard(
            "Player Search",
            "Search by player name or username to view stats, progression, and profiles.",
            Routes.Players.Root,
            new Icons.Regular.Size24.PersonSearch()),
        new FeatureCard(
            "Blogs",
            "Read the latest articles and analysis about the league and its players.",
            Routes.Articles.Blogs,
            new Icons.Regular.Size24.News()),
    ];
}
