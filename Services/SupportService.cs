using Microsoft.EntityFrameworkCore;
using VeltrexAPI.Data;
using VeltrexAPI.Dtos;
using VeltrexAPI.Entities;
using VeltrexAPI.Infrastructure;

namespace VeltrexAPI.Services;

/// <summary>
/// Support chat: one conversation per client, shared by all admins. The frontend polls
/// <see cref="GetThreadAsync"/> with the last id it has, so each poll returns only new messages.
/// </summary>
public class SupportService(AppDbContext db)
{
    /// <summary>How many recent messages the first (non-polling) load returns.</summary>
    private const int InitialLimit = 200;

    /// <param name="viewerIsAdmin">Who is reading: decides which messages count as "theirs" for ReadUpTo.</param>
    /// <param name="afterId">Only messages newer than this id (polling); null for the latest page.</param>
    public async Task<SupportThreadDto> GetThreadAsync(Guid userId, bool viewerIsAdmin, long? afterId, CancellationToken ct)
    {
        var q = db.SupportMessages.AsNoTracking().Where(m => m.UserId == userId);

        List<SupportMessage> messages = afterId is { } after
            ? await q.Where(m => m.Id > after).OrderBy(m => m.Id).Take(InitialLimit).ToListAsync(ct)
            : (await q.OrderByDescending(m => m.Id).Take(InitialLimit).ToListAsync(ct)).AsEnumerable().Reverse().ToList();

        var readUpTo = await q
            .Where(m => m.FromAdmin == viewerIsAdmin && m.ReadAt != null)
            .MaxAsync(m => (long?)m.Id, ct);

        return new SupportThreadDto(messages.Select(m => m.ToDto()).ToList(), readUpTo);
    }

    public async Task<SupportMessageDto> SendAsync(Guid userId, bool fromAdmin, Guid senderId, string body, CancellationToken ct)
    {
        body = body.Trim();
        if (body.Length == 0) throw ApiException.BadRequest("Type a message");

        var message = new SupportMessage { UserId = userId, FromAdmin = fromAdmin, SenderId = senderId, Body = body };
        db.SupportMessages.Add(message);
        await db.SaveChangesAsync(ct);
        return message.ToDto();
    }

    /// <summary>Marks the other side's messages in this conversation as read.</summary>
    public Task MarkReadAsync(Guid userId, bool viewerIsAdmin, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        return db.SupportMessages
            .Where(m => m.UserId == userId && m.FromAdmin != viewerIsAdmin && m.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.ReadAt, now), ct);
    }

    /// <summary>Admin replies the client hasn't read yet.</summary>
    public async Task<UnreadCountDto> ClientUnreadAsync(Guid userId, CancellationToken ct) =>
        new(await db.SupportMessages.CountAsync(m => m.UserId == userId && m.FromAdmin && m.ReadAt == null, ct));

    /// <summary>Client messages no admin has read yet, across all conversations.</summary>
    public async Task<UnreadCountDto> AdminUnreadAsync(CancellationToken ct) =>
        new(await db.SupportMessages.CountAsync(m => !m.FromAdmin && m.ReadAt == null, ct));

    /// <summary>The admin inbox: every conversation, most recent activity first.</summary>
    public async Task<List<SupportConversationDto>> ListConversationsAsync(CancellationToken ct)
    {
        var groups = await db.SupportMessages.AsNoTracking()
            .GroupBy(m => m.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                LastId = g.Max(m => m.Id),
                Unread = g.Count(m => !m.FromAdmin && m.ReadAt == null),
            })
            .ToListAsync(ct);
        if (groups.Count == 0) return [];

        var lastIds = groups.Select(g => g.LastId).ToList();
        var lasts = await db.SupportMessages.AsNoTracking()
            .Where(m => lastIds.Contains(m.Id))
            .Select(m => new { m.Id, m.UserId, m.Body, m.FromAdmin, m.CreatedAt, m.User!.Name, m.User.ClientCode, m.User.Email })
            .ToDictionaryAsync(m => m.Id, ct);

        return groups
            .Where(g => lasts.ContainsKey(g.LastId))
            .Select(g =>
            {
                var m = lasts[g.LastId];
                var preview = m.Body.Length > 120 ? m.Body[..120] + "…" : m.Body;
                return new SupportConversationDto(g.UserId, m.Name, m.ClientCode, m.Email, preview, m.FromAdmin, m.CreatedAt, g.Unread);
            })
            .OrderByDescending(c => c.LastAt)
            .ToList();
    }
}
