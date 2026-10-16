using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VeltrexAPI.Data;
using VeltrexAPI.Dtos;
using VeltrexAPI.Entities;
using VeltrexAPI.Infrastructure;

namespace VeltrexAPI.Services;

/// <summary>
/// KYC details for a client. Clients fill it in after sign-up (or later from their profile);
/// the admin can view, correct, approve or reject it. Approving marks the account verified.
/// </summary>
public partial class KycService(AppDbContext db, AccountService accounts)
{
    public async Task<KycDto?> GetAsync(Guid userId, CancellationToken ct) =>
        (await db.KycDetails.AsNoTracking().FirstOrDefaultAsync(k => k.UserId == userId, ct))?.ToDto();

    /// <summary>Client submits (or re-submits) their details; it goes back into review.</summary>
    public async Task<KycDto> SubmitAsync(Guid userId, KycInput input, CancellationToken ct)
    {
        var kyc = await db.KycDetails.FirstOrDefaultAsync(k => k.UserId == userId, ct);
        if (kyc?.Status == KycStatus.Approved)
            throw ApiException.Conflict("Your KYC is already approved. Contact support to change your details.");

        if (kyc == null) db.KycDetails.Add(kyc = new KycDetails { UserId = userId });
        Apply(kyc, input);
        kyc.Status = KycStatus.Submitted;
        kyc.AdminNote = "";
        kyc.SubmittedAt = DateTimeOffset.UtcNow;
        kyc.ReviewedAt = null;

        await db.SaveChangesAsync(ct);
        return kyc.ToDto();
    }

    /// <summary>Admin creates or corrects a client's details. The review status is left as it is.</summary>
    public async Task<KycDto> SaveByAdminAsync(Guid userId, KycInput input, CancellationToken ct)
    {
        await accounts.GetClientAsync(userId, ct);
        var kyc = await db.KycDetails.FirstOrDefaultAsync(k => k.UserId == userId, ct);
        if (kyc == null) db.KycDetails.Add(kyc = new KycDetails { UserId = userId });
        Apply(kyc, input);

        await db.SaveChangesAsync(ct);
        return kyc.ToDto();
    }

    /// <summary>Approve or reject. Approving verifies the account; rejecting un-verifies it.</summary>
    public async Task<KycDto> DecideAsync(Guid userId, KycDecision input, CancellationToken ct)
    {
        var user = await accounts.GetClientAsync(userId, ct);
        var kyc = user.Kyc ?? throw ApiException.BadRequest("This client hasn't submitted KYC details yet");

        kyc.Status = input.Status;
        kyc.AdminNote = input.AdminNote?.Trim() ?? "";
        kyc.ReviewedAt = input.Status == KycStatus.Submitted ? null : DateTimeOffset.UtcNow;
        if (input.Status == KycStatus.Approved) user.Verified = true;
        if (input.Status == KycStatus.Rejected) user.Verified = false;

        await db.SaveChangesAsync(ct);
        return kyc.ToDto();
    }

    /* ---------- validation ---------- */

    private static void Apply(KycDetails k, KycInput i)
    {
        var phone = Compact(i.Phone);
        if (!PhoneRx().IsMatch(phone)) throw ApiException.BadRequest("Enter a valid phone number");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (i.DateOfBirth < new DateOnly(1900, 1, 1) || i.DateOfBirth > today)
            throw ApiException.BadRequest("Enter a valid date of birth");
        if (i.DateOfBirth.AddYears(18) > today)
            throw ApiException.BadRequest("You must be at least 18 years old");

        var account = Compact(i.AccountNumber);
        if (!AccountRx().IsMatch(account)) throw ApiException.BadRequest("Account number must be 6 to 18 digits");

        var ifsc = i.IfscCode.Trim().ToUpperInvariant();
        if (!IfscRx().IsMatch(ifsc)) throw ApiException.BadRequest("Enter a valid 11-character IFSC code (e.g. HDFC0001234)");

        var aadhaar = Compact(i.AadhaarNumber);
        if (!AadhaarRx().IsMatch(aadhaar)) throw ApiException.BadRequest("Aadhaar number must be 12 digits");

        var pan = i.PanNumber.Trim().ToUpperInvariant();
        if (!PanRx().IsMatch(pan)) throw ApiException.BadRequest("Enter a valid PAN (e.g. ABCDE1234F)");

        k.FirstName = i.FirstName.Trim();
        k.LastName = i.LastName.Trim();
        k.Email = i.Email.Trim();
        k.Phone = phone;
        k.DateOfBirth = i.DateOfBirth;
        k.AddressLine = i.AddressLine.Trim();
        k.City = i.City.Trim();
        k.State = i.State.Trim();
        k.Nationality = i.Nationality.Trim();
        k.BankName = i.BankName.Trim();
        k.AccountHolderName = i.AccountHolderName.Trim();
        k.AccountNumber = account;
        k.IfscCode = ifsc;
        k.AadhaarNumber = aadhaar;
        k.PanNumber = pan;
    }

    /// <summary>Removes spaces and hyphens people type into long numbers.</summary>
    private static string Compact(string s) => SeparatorsRx().Replace(s, "");

    [GeneratedRegex(@"[\s-]")] private static partial Regex SeparatorsRx();
    [GeneratedRegex(@"^\+?[0-9]{7,15}$")] private static partial Regex PhoneRx();
    [GeneratedRegex(@"^[0-9]{6,18}$")] private static partial Regex AccountRx();
    [GeneratedRegex(@"^[A-Z]{4}0[A-Z0-9]{6}$")] private static partial Regex IfscRx();
    [GeneratedRegex(@"^[2-9][0-9]{11}$")] private static partial Regex AadhaarRx();
    [GeneratedRegex(@"^[A-Z]{5}[0-9]{4}[A-Z]$")] private static partial Regex PanRx();
}
