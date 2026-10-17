using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeltrexAPI.Dtos;
using VeltrexAPI.Entities;
using VeltrexAPI.Infrastructure;
using VeltrexAPI.Services;

namespace VeltrexAPI.Controllers;

/// <summary>Support chat. Clients talk to "the support team"; any admin can read and reply.</summary>
[ApiController]
public class SupportController(SupportService support, AccountService accounts) : ControllerBase
{
    /* ---------- Client ---------- */

    /// <summary>The client's conversation. Pass <c>?afterId=</c> the last id you have to get only new messages.</summary>
    [HttpGet("api/client/support/messages")]
    [Authorize(Roles = Roles.Client)]
    public Task<SupportThreadDto> MyThread([FromQuery] long? afterId, CancellationToken ct) =>
        support.GetThreadAsync(User.GetUserId(), viewerIsAdmin: false, afterId, ct);

    [HttpPost("api/client/support/messages")]
    [Authorize(Roles = Roles.Client)]
    public Task<SupportMessageDto> Send(SupportMessageInput input, CancellationToken ct)
    {
        var me = User.GetUserId();
        return support.SendAsync(me, fromAdmin: false, senderId: me, input.Body, ct);
    }

    /// <summary>Marks the support team's replies as read.</summary>
    [HttpPost("api/client/support/read")]
    [Authorize(Roles = Roles.Client)]
    public async Task<IActionResult> MarkRead(CancellationToken ct)
    {
        await support.MarkReadAsync(User.GetUserId(), viewerIsAdmin: false, ct);
        return NoContent();
    }

    /// <summary>Unread replies, for the notification badge.</summary>
    [HttpGet("api/client/support/unread")]
    [Authorize(Roles = Roles.Client)]
    public Task<UnreadCountDto> MyUnread(CancellationToken ct) => support.ClientUnreadAsync(User.GetUserId(), ct);

    /* ---------- Admin ---------- */

    [HttpGet("api/admin/support/conversations")]
    [Authorize(Roles = Roles.Admin)]
    public Task<List<SupportConversationDto>> Conversations(CancellationToken ct) => support.ListConversationsAsync(ct);

    /// <summary>Unread client messages across all conversations, for the sidebar badge.</summary>
    [HttpGet("api/admin/support/unread")]
    [Authorize(Roles = Roles.Admin)]
    public Task<UnreadCountDto> AdminUnread(CancellationToken ct) => support.AdminUnreadAsync(ct);

    [HttpGet("api/admin/users/{userId:guid}/support/messages")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<SupportThreadDto> ClientThread(Guid userId, [FromQuery] long? afterId, CancellationToken ct)
    {
        await accounts.GetClientAsync(userId, ct);
        return await support.GetThreadAsync(userId, viewerIsAdmin: true, afterId, ct);
    }

    [HttpPost("api/admin/users/{userId:guid}/support/messages")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<SupportMessageDto> Reply(Guid userId, SupportMessageInput input, CancellationToken ct)
    {
        await accounts.GetClientAsync(userId, ct);
        return await support.SendAsync(userId, fromAdmin: true, senderId: User.GetUserId(), input.Body, ct);
    }

    [HttpPost("api/admin/users/{userId:guid}/support/read")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> MarkClientRead(Guid userId, CancellationToken ct)
    {
        await accounts.GetClientAsync(userId, ct);
        await support.MarkReadAsync(userId, viewerIsAdmin: true, ct);
        return NoContent();
    }
}
