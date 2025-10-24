using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EVChargingBackend.Models;

/// <summary>
/// Represents schedule overrides for a charging station on specific dates
/// </summary>
public class StationScheduleOverride
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID of the charging station
    /// </summary>
    [BsonElement("station_id")]
    public string StationId { get; set; } = string.Empty;

    /// <summary>
    /// Date for this override (YYYY-MM-DD format)
    /// </summary>
    [BsonElement("date")]
    public DateTime Date { get; set; }

    /// <summary>
    /// Whether the station is closed for this entire day
    /// </summary>
    [BsonElement("closed")]
    public bool Closed { get; set; } = false;

    /// <summary>
    /// Special opening time for this date (overrides station default)
    /// </summary>
    [BsonElement("open_time")]
    public TimeSpan? OpenTime { get; set; }

    /// <summary>
    /// Special closing time for this date (overrides station default)
    /// </summary>
    [BsonElement("close_time")]
    public TimeSpan? CloseTime { get; set; }

    /// <summary>
    /// Maintenance windows for this date
    /// </summary>
    [BsonElement("maintenance_windows")]
    public List<MaintenanceWindow> MaintenanceWindows { get; set; } = new();

    /// <summary>
    /// When this override was created
    /// </summary>
    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this override was last updated
    /// </summary>
    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created this override
    /// </summary>
    [BsonElement("created_by")]
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Represents a maintenance window within a day
/// </summary>
public class MaintenanceWindow
{
    /// <summary>
    /// Start time of the maintenance window
    /// </summary>
    [BsonElement("start_time")]
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// End time of the maintenance window
    /// </summary>
    [BsonElement("end_time")]
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// Reason for the maintenance
    /// </summary>
    [BsonElement("reason")]
    public string Reason { get; set; } = string.Empty;
}
