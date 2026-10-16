using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeltrexAPI.Dtos;
using VeltrexAPI.Entities;
using VeltrexAPI.Infrastructure;
using VeltrexAPI.Services;

namespace VeltrexAPI.Controllers;

/// <summary>Deposit screenshots: clients upload, admins review.</summary>
[ApiController]
public class PaymentProofsController(PaymentProofService proofs, AccountService accounts) : ControllerBase
{
    /* ---------- Client ---------- */

    [HttpGet("api/client/payment-proofs")]
    [Authorize(Roles = Roles.Client)]
    public Task<List<PaymentProofDto>> Mine(CancellationToken ct) => proofs.ListForClientAsync(User.GetUserId(), ct);

    /// <summary>multipart/form-data: image (PNG/JPG/WEBP/GIF, max 5 MB), amount (optional), reference (optional UTR).</summary>
    [HttpPost("api/client/payment-proofs")]
    [Authorize(Roles = Roles.Client)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(PaymentProofService.MaxBytes + 512 * 1024)]
    public Task<PaymentProofDto> Upload([FromForm] PaymentProofUploadForm form, CancellationToken ct) =>
        proofs.UploadAsync(User.GetUserId(), form, ct);

    [HttpGet("api/client/payment-proofs/{proofId:guid}/image")]
    [Authorize(Roles = Roles.Client)]
    public async Task<IActionResult> MyImage(Guid proofId, CancellationToken ct) =>
        Image(await proofs.GetImageAsync(proofId, User.GetUserId(), ct));

    /* ---------- Admin ---------- */

    /// <summary>All clients' screenshots, newest first. <c>?status=PENDING</c> for the review queue.</summary>
    [HttpGet("api/admin/payment-proofs")]
    [Authorize(Roles = Roles.Admin)]
    public Task<List<PaymentProofDto>> All([FromQuery] PaymentProofStatus? status, CancellationToken ct) =>
        proofs.ListAllAsync(status, ct);

    [HttpGet("api/admin/users/{userId:guid}/payment-proofs")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<List<PaymentProofDto>> ForClient(Guid userId, CancellationToken ct)
    {
        await accounts.GetClientAsync(userId, ct);
        return await proofs.ListForClientAsync(userId, ct);
    }

    [HttpGet("api/admin/users/{userId:guid}/payment-proofs/{proofId:guid}/image")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> ClientImage(Guid userId, Guid proofId, CancellationToken ct) =>
        Image(await proofs.GetImageAsync(proofId, userId, ct));

    /// <summary><c>{ status, adminNote }</c>. Approving does not credit the ledger; do that from the Ledger tab.</summary>
    [HttpPut("api/admin/users/{userId:guid}/payment-proofs/{proofId:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public Task<PaymentProofDto> Decide(Guid userId, Guid proofId, PaymentProofDecision input, CancellationToken ct) =>
        proofs.DecideAsync(userId, proofId, input, ct);

    private FileContentResult Image((byte[] Data, string ContentType) img)
    {
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.CacheControl = "private, max-age=86400"; // screenshots never change
        return File(img.Data, img.ContentType);
    }
}
