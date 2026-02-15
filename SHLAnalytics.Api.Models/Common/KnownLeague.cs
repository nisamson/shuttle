namespace SHLAnalytics.Api.Models.Common;

public enum KnownLeague {
    Shl = 0,
    Smjhl = 1,
    Iihf = 2,
}

public static class KnownLeagueExtensions {
    extension(KnownLeague league) {
        public string Abbreviation =>
            league switch {
                KnownLeague.Shl => "SHL",
                KnownLeague.Smjhl => "SMJHL",
                KnownLeague.Iihf => "IIHF",
                _ => throw new ArgumentOutOfRangeException(nameof(league), $"Not expected league value: {league}")
            };

        public string Name =>
            league switch {
                KnownLeague.Shl => "Simulation Hockey League",
                KnownLeague.Smjhl => "Simulation Major Junior Hockey League",
                KnownLeague.Iihf => "International Ice Hockey Federation",
                _ => throw new ArgumentOutOfRangeException(nameof(league), $"Not expected league value: {league}")
            };

        public int Id => (int)league;

        public static KnownLeague FromId(int id) =>
            id switch {
                0 => KnownLeague.Shl,
                1 => KnownLeague.Smjhl,
                2 => KnownLeague.Iihf,
                _ => throw new ArgumentOutOfRangeException(nameof(id), $"Not expected league id: {id}")
            };

        public static KnownLeague FromAbbreviation(string abbreviation) =>
            abbreviation switch {
                "SHL" => KnownLeague.Shl,
                "SMJHL" => KnownLeague.Smjhl,
                "IIHF" => KnownLeague.Iihf,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(abbreviation),
                    $"Not expected league abbreviation: {abbreviation}"
                )
            };
    }
}