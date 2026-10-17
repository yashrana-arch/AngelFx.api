using Microsoft.EntityFrameworkCore;
using VeltrexAPI.Data;
using VeltrexAPI.Dtos;
using VeltrexAPI.Entities;
using VeltrexAPI.Infrastructure;

namespace VeltrexAPI.Services;

/// <summary>
/// Deposit screenshots. Clients upload them after paying; the admin reviews them and credits the
/// ledger by hand. Images are stored in the PaymentProofs table, never on disk.
/// </summary>
public class PaymentProofService(AppDbContext db, AccountService accounts)
{
    public const int MaxBytes = 5 * 1024 * 1024;
    private const int MaxPendingPerClient = 20;

    /* ---------- Lists (metadata only, never the image bytes) ---------- */

    public Task<List<PaymentProofDto>> ListForClientAsync(Guid userId, CancellationToken ct) =>
        Project(db.PaymentProofs.Where(p => p.UserId == userId)).ToListAsync(ct);

    /// <summary>Across all clients, newest first; optionally only one status (e.g. the pending queue).</summary>
    public Task<List<PaymentProofDto>> ListAllAsync(PaymentProofStatus? status, CancellationToken ct) =>
        Project(status == null ? db.PaymentProofs : db.PaymentProofs.Where(p => p.Status == status))
            .Take(300)
            .ToListAsync(ct);

    private static IQueryable<PaymentProofDto> Project(IQueryable<PaymentProof> q) =>
        q.AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentProofDto(
                p.Id, p.UserId, p.User!.Name, p.User.ClientCode,
                p.Amount, p.Reference, p.FileName, p.FileSize,
                p.Status, p.AdminNote, p.CreatedAt, p.ReviewedAt));

    /* ---------- Upload ---------- */

    public async Task<PaymentProofDto> UploadAsync(Guid userId, PaymentProofUploadForm form, CancellationToken ct)
    {
        var file = form.Image;
        if (file == null || file.Length == 0) throw ApiException.BadRequest("Please choose a screenshot to upload");
        if (file.Length > MaxBytes) throw ApiException.BadRequest("Image must be under 5 MB");

        var pending = await db.PaymentProofs.CountAsync(p => p.UserId == userId && p.Status == PaymentProofStatus.Pending, ct);
        if (pending >= MaxPendingPerClient)
            throw ApiException.BadRequest("You have too many screenshots waiting for review. Please wait for them to be checked.");

        using var ms = new MemoryStream((int)file.Length);
        await file.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        // Trust the file's actual bytes, not the name or browser-reported type.
        var contentType = DetectImageType(bytes)
            ?? throw ApiException.BadRequest("Please upload a PNG, JPG, WEBP or GIF image");

        var proof = new PaymentProof
        {
            UserId = userId,
            ImageData = bytes,
            ContentType = contentType,
            FileName = SafeFileName(file.FileName),
            FileSize = bytes.Length,
            Amount = form.Amount,
            Reference = form.Reference?.Trim() ?? "",
        };
        db.PaymentProofs.Add(proof);
        await db.SaveChangesAsync(ct);

        return (await Project(db.PaymentProofs.Where(p => p.Id == proof.Id)).FirstAsync(ct));
    }

    /* ---------- Image ---------- */

    /// <summary>The image bytes. Pass <paramref name="userId"/> to scope to one client.</summary>
    public async Task<(byte[] Data, string ContentType)> GetImageAsync(Guid proofId, Guid? userId, CancellationToken ct)
    {
        var img = await db.PaymentProofs.AsNoTracking()
            .Where(p => p.Id == proofId && (userId == null || p.UserId == userId))
            .Select(p => new { p.ImageData, p.ContentType })
            .FirstOrDefaultAsync(ct)
            ?? throw ApiException.NotFound("Screenshot not found");
        return (img.ImageData, img.ContentType);
    }

    /* ---------- Admin review ---------- */

    public async Task<PaymentProofDto> DecideAsync(Guid userId, Guid proofId, PaymentProofDecision input, CancellationToken ct)
    {
        await accounts.GetClientAsync(userId, ct);
        var proof = await db.PaymentProofs.FirstOrDefaultAsync(p => p.Id == proofId && p.UserId == userId, ct)
            ?? throw ApiException.NotFound("Screenshot not found");

        proof.Status = input.Status;
        proof.AdminNote = input.AdminNote?.Trim() ?? "";
        proof.ReviewedAt = input.Status == PaymentProofStatus.Pending ? null : DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return await Project(db.PaymentProofs.Where(p => p.Id == proof.Id)).FirstAsync(ct);
    }

    /* ---------- Delete ---------- */

    /// <summary>
    /// Client removes their own screenshot, e.g. a wrong image. Only while it's still pending:
    /// once reviewed it stays as the record behind the admin's decision.
    /// </summary>
    public async Task DeleteByClientAsync(Guid userId, Guid proofId, CancellationToken ct)
    {
        var status = await db.PaymentProofs
            .Where(p => p.Id == proofId && p.UserId == userId)
            .Select(p => (PaymentProofStatus?)p.Status)
            .FirstOrDefaultAsync(ct)
            ?? throw ApiException.NotFound("Screenshot not found");
        if (status != PaymentProofStatus.Pending)
            throw ApiException.Conflict("This screenshot has already been reviewed, so it can't be deleted");

        // Re-check the status in the delete itself, in case an admin reviewed it in the meantime.
        var deleted = await db.PaymentProofs
            .Where(p => p.Id == proofId && p.UserId == userId && p.Status == PaymentProofStatus.Pending)
            .ExecuteDeleteAsync(ct);
        if (deleted == 0)
            throw ApiException.Conflict("This screenshot has already been reviewed, so it can't be deleted");
    }

    /// <summary>Admin removes any screenshot (spam, duplicates, test uploads). Doesn't touch the ledger.</summary>
    public async Task DeleteByAdminAsync(Guid userId, Guid proofId, CancellationToken ct)
    {
        await accounts.GetClientAsync(userId, ct);
        var deleted = await db.PaymentProofs.Where(p => p.Id == proofId && p.UserId == userId).ExecuteDeleteAsync(ct);
        if (deleted == 0) throw ApiException.NotFound("Screenshot not found");
    }

    /* ---------- helpers ---------- */

    private static string? DetectImageType(byte[] b)
    {
        if (b.Length < 12) return null;
        if (b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return "image/png";
        if (b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "image/jpeg";
        if (b[0] == 'G' && b[1] == 'I' && b[2] == 'F' && b[3] == '8') return "image/gif";
        if (b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F' && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P') return "image/webp";
        return null;
    }

    private static string SafeFileName(string name)
    {
        var clean = Path.GetFileName(name ?? "").Trim();
        if (clean.Length == 0) return "screenshot";
        return clean.Length > 255 ? clean[^255..] : clean;
    }
}
