namespace AdmissionSystem.Api.Entities;

// One application for a seat. It starts as HELD and later becomes
// CONFIRMED, CANCELLED or EXPIRED.
public class Hold
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string Status { get; set; } = HoldStatus.Held;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    public AdmissionClass? Class { get; set; }
}

// Status values are stored as text in the database.
public static class HoldStatus
{
    public const string Held = "HELD";
    public const string Confirmed = "CONFIRMED";
    public const string Expired = "EXPIRED";
    public const string Cancelled = "CANCELLED";

    // A HELD hold whose expiry time has passed is reported as EXPIRED,
    // even if nobody has updated the stored status yet.
    public static string Effective(Hold hold, DateTime nowUtc)
    {
        if (hold.Status == Held && hold.ExpiresAt <= nowUtc)
        {
            return Expired;
        }
        return hold.Status;
    }
}
