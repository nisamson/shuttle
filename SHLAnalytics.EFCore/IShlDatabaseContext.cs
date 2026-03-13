using Microsoft.EntityFrameworkCore;
using SHLAnalytics.EFCore.Entities;
using SHLAnalytics.EFCore.Entities.Portal;
using SHLAnalytics.EFCore.SiteArchive;

namespace SHLAnalytics.EFCore;

public interface IShlDatabaseContext {
    DbSet<PlayerInfo> PlayerInfos { get; set; }
}
