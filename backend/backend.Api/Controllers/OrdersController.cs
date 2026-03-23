using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace backend.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public OrdersController(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetOrders(CancellationToken ct)
    {
        var response = await _httpClient.GetAsync("http://localhost:5003/api/orders", ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var orders = await response.Content.ReadFromJsonAsync<IReadOnlyList<OrderDto>>(ct);
        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetOrderById(Guid id, CancellationToken ct)
    {
        var response = await _httpClient.GetAsync($"http://localhost:5003/api/orders/{id}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var order = await response.Content.ReadFromJsonAsync<OrderDto>(ct);
        return order == null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderRequest request, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync("http://localhost:5003/api/orders", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var order = await response.Content.ReadFromJsonAsync<OrderDto>(ct);
        return order == null ? NotFound() : CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OrderDto>> UpdateOrder(Guid id, UpdateOrderRequest request, CancellationToken ct)
    {
        var response = await _httpClient.PutAsJsonAsync($"http://localhost:5003/api/orders/{id}", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var order = await response.Content.ReadFromJsonAsync<OrderDto>(ct);
        return Ok(order);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteOrder(Guid id, CancellationToken ct)
    {
        var response = await _httpClient.DeleteAsync($"http://localhost:5003/api/orders/{id}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        return NoContent();
    }
}

public sealed record OrderDto(
    Guid Id,
    Guid UserId,
    string OrderNumber,
    decimal TotalAmount,
    DateTime CreatedAtUtc
);

public sealed record CreateOrderRequest(
    Guid UserId,
    string OrderNumber,
    decimal TotalAmount
);

public sealed record UpdateOrderRequest(
    string OrderNumber,
    decimal TotalAmount
);
