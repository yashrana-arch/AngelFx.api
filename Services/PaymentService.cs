using Microsoft.EntityFrameworkCore;
using VeltrexAPI.Data;
using VeltrexAPI.Dtos;
using VeltrexAPI.Entities;

namespace VeltrexAPI.Services;

/// <summary>Reads and writes the deposit details shared by all clients (UPI QR + bank account).</summary>
public class PaymentService(AppDbContext db, QrStorage qrStorage)
{
    public async Task<PaymentInfoDto> GetAsync(string baseUrl, CancellationToken ct)
    {
        var p = await db.PaymentSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == PaymentSettings.SingletonId, ct);
        return ToDto(p, baseUrl);
    }

    public async Task<PaymentInfoDto> SaveAsync(PaymentUpdateForm form, string baseUrl, CancellationToken ct)
    {
        var p = await db.PaymentSettings.FirstOrDefaultAsync(x => x.Id == PaymentSettings.SingletonId, ct);
        if (p == null) db.PaymentSettings.Add(p = new PaymentSettings());
        p.UpiId = Clean(form.UpiId);
        p.BankName = Clean(form.BankName);
        p.AccountHolderName = Clean(form.AccountHolderName);
        p.AccountNumber = Clean(form.AccountNumber);
        p.IfscCode = Clean(form.IfscCode).ToUpperInvariant();
        p.Note = Clean(form.Note);
        p.UpdatedAt = DateTimeOffset.UtcNow;

        string? oldImage = null;
        if (form.QrImage is { Length: > 0 })
        {
            oldImage = p.QrImagePath;
            p.QrImagePath = await qrStorage.SaveAsync(form.QrImage, ct);
        }

        await db.SaveChangesAsync(ct);
        qrStorage.Delete(oldImage);
        return ToDto(p, baseUrl);
    }

    private static PaymentInfoDto ToDto(PaymentSettings? p, string baseUrl) =>
        new(p?.QrImagePath is { } path ? $"{baseUrl}/{path}" : null,
            p?.UpiId ?? "", p?.BankName ?? "", p?.AccountHolderName ?? "",
            p?.AccountNumber ?? "", p?.IfscCode ?? "", p?.Note ?? "");

    private static string Clean(string? s) => s?.Trim() ?? "";
}
