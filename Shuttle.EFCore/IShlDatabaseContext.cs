using Microsoft.EntityFrameworkCore;
using Shuttle.EFCore.Entities;
using Shuttle.EFCore.SiteArchive;
using Shuttle.EFCore.Entities.Portal;

namespace Shuttle.EFCore;

public interface IShlDatabaseContext {
    DbSet<PlayerInfo> PlayerInfos { get; set; }
}
