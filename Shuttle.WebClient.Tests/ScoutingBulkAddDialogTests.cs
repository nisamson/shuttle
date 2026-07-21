using Shuttle.WebClient.Components.Scouting;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Tests for the "search and add a player" flow on <see cref="ScoutingBulkAddDialog"/>: picking a
/// player appends its (unambiguous) id to the paste box on a new line, and repeated picks of the same
/// player must not duplicate a line.
/// </summary>
public class ScoutingBulkAddDialogTests {
    [Fact]
    public void Appends_an_id_to_an_empty_paste_box() {
        Assert.Equal("1042", ScoutingBulkAddDialog.AppendPlayerId(string.Empty, 1042));
    }

    [Fact]
    public void Appends_an_id_on_a_new_line_after_existing_content() {
        Assert.Equal("Wayne Gretzky\n1099",
            ScoutingBulkAddDialog.AppendPlayerId("Wayne Gretzky", 1099));
    }

    [Fact]
    public void Does_not_add_a_trailing_blank_line_when_the_box_ends_with_a_newline() {
        Assert.Equal("1042\n1099",
            ScoutingBulkAddDialog.AppendPlayerId("1042\n", 1099));
    }

    [Fact]
    public void Does_not_duplicate_an_id_already_present() {
        Assert.Equal("1042\n1099",
            ScoutingBulkAddDialog.AppendPlayerId("1042\n1099", 1042));
    }
}
