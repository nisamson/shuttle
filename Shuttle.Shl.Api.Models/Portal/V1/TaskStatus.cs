using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shuttle.Shl.Api.Models.Portal.V1;

[JsonConverter(typeof(JsonStringEnumConverter<TaskStatus>))]
public enum TaskStatus {
    [JsonStringEnumMemberName("Draftee Free Agent")]
    DrafteeFreeAgent,
    [JsonStringEnumMemberName("SMJHL Rookie")]
    SmjhlRookie,
    [JsonStringEnumMemberName("SHL/Send-down")]
    ShlOrSendDown,
    [JsonStringEnumMemberName("Retired")]
    Retired,
    // Appended (not reordered): TaskStatus is persisted as an int on PlayerInformation,
    // so existing ordinal values (0-3) must stay stable. JSON maps by name, not ordinal.
    [JsonStringEnumMemberName("Pending Approval")]
    PendingApproval
}

public static class TaskStatusExtensions {
    extension(TaskStatus @this) {
        public string ToValueString() => @this switch {
            TaskStatus.PendingApproval => "Pending Approval",
            TaskStatus.DrafteeFreeAgent => "Draftee Free Agent",
            TaskStatus.SmjhlRookie => "SMJHL Rookie",
            TaskStatus.ShlOrSendDown => "SHL/Send-down",
            TaskStatus.Retired => "Retired",
            _ => throw new ArgumentOutOfRangeException(nameof(@this), @this, null)
        };
        
        public static TaskStatus FromString(string status) => status.ToLower() switch {
            "pending approval" => TaskStatus.PendingApproval,
            "draftee free agent" => TaskStatus.DrafteeFreeAgent,
            "smjhl rookie" => TaskStatus.SmjhlRookie,
            "shl/send-down" => TaskStatus.ShlOrSendDown,
            "retired" => TaskStatus.Retired,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }
}

public class TaskStatusJsonConverter : JsonConverter<TaskStatus> {

    public override TaskStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var text = reader.GetString();
        if (text is null) {
            throw new JsonException("TaskStatus value is null");
        }
        return TaskStatus.FromString(text);
    }
    
    public override void Write(Utf8JsonWriter writer, TaskStatus value, JsonSerializerOptions options) {
        writer.WriteStringValue(value.ToValueString());
    }
}

