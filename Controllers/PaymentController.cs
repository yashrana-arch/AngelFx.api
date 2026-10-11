using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeltrexAPI.Dtos;
using VeltrexAPI.Entities;
using VeltrexAPI.Services;

namespace VeltrexAPI.Controllers;

[ApiController]
[Authorize]
public class PaymentController(PaymentService payment) : ControllerBase
{
    private string BaseUrl => $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

    /// <summary>Deposit details (UPI QR + bank account) shown to every client.</summary>
    [HttpGet("api/payment")]
    public Task<PaymentInfoDto> Get(CancellationToken ct) => payment.GetAsync(BaseUrl, ct);

    /// <summary>multipart/form-data: upiId, bankName, accountHolderName, accountNumber, ifscCode, note, qrImage (optional file). Admin only.</summary>
    [HttpPut("api/admin/payment")]
    [Authorize(Roles = Roles.Admin)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public Task<PaymentInfoDto> Save([FromForm] PaymentUpdateForm form, CancellationToken ct) =>
        payment.SaveAsync(form, BaseUrl, ct);
}
