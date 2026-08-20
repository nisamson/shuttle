# FHM 10 save-folder SQLite format

`Shuttle.Fhm.Serde.Sqlite` is a portable local editing boundary for an FHM
10 folder. It is not the production SHL analytics database. The adapter applies
its EF Core migrations whenever it opens a database; the singleton
`SaveManifest` identifies the save-format schema version.

The save reader, writer, files, and editable models are
`Shuttle.Fhm.Serde.Domain.*`. Domain maps to the
`Shuttle.Fhm.Serde.Wire.*` BinarySerializer Qt contracts; Wire has no Domain
dependency.

## Contract

Import stores one `SaveFiles` baseline row per normalized source path. The row
contains its exact bytes and whether the source was a documented or opaque
file. Unedited export returns documented baseline files as raw byte containers
and restores opaque files directly; therefore a save-folder → SQLite →
save-folder conversion is byte-equivalent.

Projection edits are update-only. Export compares source key and ordinal sets
with the parsed baseline and rejects inserts, deletes, reorders, identity
changes, invalid dates/ratings/settings, invalid references, or malformed
fixed-size payloads. The check applies equally to EF Core and direct-SQL
edits. A rejected export returns no `FhmSave` for writing.

The editable boundary includes names; player profiles, positions, and rating
vectors; team profiles and embedded tactical settings; game settings; stored
lines; tactic systems/templates; set plays; tactics; and the supported
modifier catalogues. Fields outside those projections, all opaque files, and
the player serialized-record backing remain baseline-preserved.

## Component and projection diagram

```mermaid
classDiagram
    direction LR

    class FhmSaveReader {
        +Read(sourceDirectory) FhmSave
    }
    class FhmSaveWriter {
        +Write(save, destinationDirectory)
    }
    class FhmSave {
        +Files IDictionary~string, IFhmSaveFile~
        +OpaqueFiles IList~FhmOpaqueFile~
    }
    class IFhmSaveFile {
        <<interface>>
        +RelativePath string
        +WriteTo(stream)
    }
    class FhmOpaqueFile {
        +RelativePath string
        +Content byte[]
    }
    class DomainFiles {
        <<Domain>>
        +Editable save-file models
        +Lossless codecs and mappers
    }
    class WireContracts {
        <<Wire>>
        +BinarySerializer Qt contracts
    }

    class FhmSaveSqliteWriter {
        +WriteAsync(save, databasePath)
    }
    class FhmSaveSqliteReader {
        +ReadAsync(databasePath) FhmSave
    }
    class FhmSaveSqliteContext {
        +Manifests
        +Files
        +Names
        +Players
        +PlayerAttributes
        +Teams
        +GameSettings
        +StoredLines
        +TacticSystems
        +TacticTemplates
        +SetPlays
        +ModifierCatalogues
    }

    class SaveManifest {
        +SchemaVersion int
        +SourceFormatVersion string
    }
    class SaveFile {
        +RelativePath string
        +Kind SaveFileKind
        +Content byte[]
    }
    class NameProjection {
        +CollectionKind string
        +Ordinal int
        +NameId int
        +Text string
    }
    class PlayerProjection {
        +InternalId int
        +RecordOrdinal int
        +ExternalId int
        +SerializedRecord byte[]
    }
    class PlayerAttributesProjection {
        +RecordOrdinal int
        +Rating columns
    }
    class TeamProjection {
        +RecordOrdinal int
        +TeamId int
        +City string
        +Nickname string
    }
    class GameSettingProjection {
        +SettingOrdinal int
        +ValueKind string
        +IntegerValue long
        +RealValue double
        +TextValue string
    }
    class StoredLineProjection {
        +LineOrdinal int
        +Name string
    }
    class TacticProjection {
        +FilePath string
        +RecordOrdinal int
        +RawEnumValues
        +SerializedBacking byte[]
    }

    FhmSaveReader --> FhmSave : parses folder
    FhmSaveWriter --> FhmSave : serializes folder
    FhmSave --> IFhmSaveFile : modeled files
    FhmSave --> FhmOpaqueFile : unmodeled files
    FhmSaveReader --> DomainFiles : builds
    FhmSaveWriter --> DomainFiles : writes
    DomainFiles --> WireContracts : maps to/from

    FhmSaveSqliteWriter --> FhmSave : imports
    FhmSaveSqliteWriter --> FhmSaveSqliteContext : persists transactionally
    FhmSaveSqliteReader --> FhmSaveSqliteContext : validates and reads
    FhmSaveSqliteReader --> FhmSave : rehydrates and overlays edits

    FhmSaveSqliteContext --> SaveManifest
    FhmSaveSqliteContext --> SaveFile : lossless baseline
    FhmSaveSqliteContext --> NameProjection : editable
    FhmSaveSqliteContext --> PlayerProjection : editable
    FhmSaveSqliteContext --> PlayerAttributesProjection : editable
    FhmSaveSqliteContext --> TeamProjection : editable
    FhmSaveSqliteContext --> GameSettingProjection : editable
    FhmSaveSqliteContext --> StoredLineProjection : editable
    FhmSaveSqliteContext --> TacticProjection : editable

    PlayerProjection --> PlayerAttributesProjection : 1 to 1
    PlayerProjection --> SaveFile : players.dat backing
    NameProjection --> SaveFile : names.dat backing
    TeamProjection --> SaveFile : teams.dat backing
    GameSettingProjection --> SaveFile : game_settings.dat backing
    StoredLineProjection --> SaveFile : stored_lines.dat backing
    TacticProjection --> SaveFile : tactic-related backing
```

## Usage

```csharp
var source = new FhmSaveReader().Read(@"C:\saves\Example.lg");
await new FhmSaveSqliteWriter().WriteAsync(source, @"C:\work\example.sqlite");

await using var db = new FhmSaveSqliteContext(
    FhmSaveSqliteContext.CreateOptions(@"C:\work\example.sqlite"));
db.Players.Single(player => player.RecordOrdinal == 0).ExternalId = 12345;
await db.SaveChangesAsync();

var save = await new FhmSaveSqliteReader().ReadAsync(@"C:\work\example.sqlite");
new FhmSaveWriter().Write(save, @"C:\saves\Example-copy.lg");
```

The adapter never needs a temporary folder to parse baseline blobs: it uses
the Domain assembly's shared in-memory codec seam.
