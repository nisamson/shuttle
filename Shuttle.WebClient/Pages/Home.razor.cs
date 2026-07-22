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
            "Player Comparison",
            "Overlay multiple players' in-game attributes and TPE progression on shared charts.",
            Routes.Players.Compare,
            new Icons.Regular.Size24.ArrowSwap()),
        new FeatureCard(
            "Scouting Teams",
            "Build and rank shared draft boards with your scouting team. Jump to your dashboard.",
            Routes.Scouting.Root,
            new Icons.Regular.Size24.ClipboardTextEdit()),
        new FeatureCard(
            "User Search",
            "Search for members to see their profile, Discord name, and the players they've created.",
            Routes.Users.Root,
            new Icons.Regular.Size24.PeopleSearch()),
        new FeatureCard(
            "Blogs",
            "Read the latest articles and analysis about the league and its players.",
            Routes.Articles.Blogs,
            new Icons.Regular.Size24.News()),
    ];
}
