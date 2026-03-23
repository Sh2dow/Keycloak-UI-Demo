using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace backend.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public PaymentsController(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> GetPayments(CancellationToken ct)
    {
        var response = await _httpClient.GetAsync("http://localhost:5004/api/payments", ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var payments = await response.Content.ReadFromJsonAsync<IReadOnlyList<PaymentDto>>(ct);
        return Ok(payments);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentDto>> GetPaymentById(Guid id, CancellationToken ct)
    {
        var response = await _httpClient.GetAsync($"http://localhost:5004/api/payments/{id}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var payment = await response.Content.ReadFromJsonAsync<PaymentDto>(ct);
        return payment == null ? NotFound() : Ok(payment);
    }

    [HttpPost]
    public async Task<ActionResult<PaymentDto>> CreatePayment(CreatePaymentRequest request, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync("http://localhost:5004/api/payments", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var payment = await response.Content.ReadFromJsonAsync<PaymentDto>(ct);
        return payment == null ? NotFound() : CreatedAtAction(nameof(GetPaymentById), new { id = payment.Id }, payment);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PaymentDto>> UpdatePayment(Guid id, UpdatePaymentRequest request, CancellationToken ct)
    {
        var response = await _httpClient.PutAsJsonAsync($"http://localhost:5004/api/payments/{id}", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var payment = await response.Content.ReadFromJsonAsync<PaymentDto>(ct);
        return Ok(payment);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePayment(Guid id, CancellationToken ct)
    {
        var response = await _httpClient.DeleteAsync($"http://localhost:5004/api/payments/{id}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        return NoContent();
    }
}

public sealed record PaymentDto(
    Guid Id,
    Guid UserId,
    decimal Amount,
    string Status,
    DateTime CreatedAtUtc
);

public sealed record CreatePaymentRequest(
    Guid UserId,
    decimal Amount,
    string Status
);

public sealed record UpdatePaymentRequest(
    decimal Amount,
    string Status
);
