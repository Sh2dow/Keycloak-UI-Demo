using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace backend.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OrdersController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private async Task<HttpResponseMessage> ForwardRequestAsync(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("Orders");
        var request = new HttpRequestMessage(method, url);
        
        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());
        }
        
        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }
        
        return await client.SendAsync(request, ct);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetOrders(CancellationToken ct)
    {
        var response = await ForwardRequestAsync(HttpMethod.Get, "http://localhost:5003/api/orders", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var orders = await response.Content.ReadFromJsonAsync<IReadOnlyList<OrderViewDto>>(ct);
        if (orders == null) return Ok(Array.Empty<OrderDto>());
        return Ok(orders.Select(o => o.ToDto()).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetOrderById(Guid id, CancellationToken ct)
    {
        var response = await ForwardRequestAsync(HttpMethod.Get, $"http://localhost:5003/api/orders/{id}", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var order = await response.Content.ReadFromJsonAsync<OrderViewDto>(ct);
        return order == null ? NotFound() : Ok(order.ToDto());
    }

    [HttpGet("{id:guid}/workflow")]
    public async Task<IActionResult> GetOrderWorkflow(Guid id, CancellationToken ct)
    {
        var response = await ForwardRequestAsync(HttpMethod.Get, $"http://localhost:5003/api/orders/{id}/workflow", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var content = await response.Content.ReadAsStringAsync(ct);
        return Content(content, "application/json");
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderRequest request, CancellationToken ct)
    {
        var createRequest = new CreateOrderViewRequest(request.OrderType, request.TotalAmount, request.DownloadUrl, request.ShippingAddress, request.TrackingNumber);
        var response = await ForwardRequestAsync(HttpMethod.Post, "http://localhost:5003/api/orders", createRequest, ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var order = await response.Content.ReadFromJsonAsync<OrderViewDto>(ct);
        return order == null ? NotFound() : CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order.ToDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OrderDto>> UpdateOrder(Guid id, UpdateOrderRequest request, CancellationToken ct)
    {
        var response = await ForwardRequestAsync(HttpMethod.Put, $"http://localhost:5003/api/orders/{id}", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var order = await response.Content.ReadFromJsonAsync<OrderViewDto>(ct);
        return order == null ? NotFound() : Ok(order.ToDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteOrder(Guid id, CancellationToken ct)
    {
        var response = await ForwardRequestAsync(HttpMethod.Delete, $"http://localhost:5003/api/orders/{id}", null, ct);
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
    string OrderType,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAtUtc,
    string? DownloadUrl,
    string? ShippingAddress,
    string? TrackingNumber
);

public sealed record OrderViewDto(
    Guid Id,
    string OrderType,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAtUtc,
    string? DownloadUrl,
    string? ShippingAddress,
    string? TrackingNumber
)
{
    public OrderDto ToDto() => new(Id, Guid.Empty, OrderType, TotalAmount, Status, CreatedAtUtc, DownloadUrl, ShippingAddress, TrackingNumber);
}

public sealed record CreateOrderRequest(
    string OrderType,
    decimal TotalAmount,
    string? DownloadUrl,
    string? ShippingAddress,
    string? TrackingNumber
);

public sealed record CreateOrderViewRequest(
    string OrderType,
    decimal TotalAmount,
    string? DownloadUrl,
    string? ShippingAddress,
    string? TrackingNumber
);

public sealed record UpdateOrderRequest(
    string OrderNumber,
    decimal TotalAmount
);
