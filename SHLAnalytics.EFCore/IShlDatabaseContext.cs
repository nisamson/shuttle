using Microsoft.EntityFrameworkCore;
using SHLAnalytics.EFCore.SiteArchive;

namespace SHLAnalytics.EFCore;

public interface IShlDatabaseContext {
    DbSet<ArchiveEntry> ArchiveEntries { get; set; }
}
