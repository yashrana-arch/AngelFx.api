namespace VeltrexAPI.Entities;

/// <summary>
/// One message in a client's support chat. Each client has a single conversation with the
/// support team, identified by <see cref="UserId"/>; any admin can reply in it.
/// </summary>
public class SupportMessage
{
    /// <summary>Auto-increment, so clients can poll for "messages after id N".</summary>
    public long Id { get; set; }

    /// <summary>The client whose conversation this belongs to (also for admin replies).</summary>
    public Guid UserId { get; set; }

    /// <summary>True when an admin sent it, false when the client did.</summary>
    public bool FromAdmin { get; set; }

    /// <summary>Who actually sent it: the client, or the replying admin.</summary>
    public Guid SenderId { get; set; }

    public string Body { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When the other side read it; null while unread.</summary>
    public DateTimeOffset? ReadAt { get; set; }

    public User? User { get; set; }
}
