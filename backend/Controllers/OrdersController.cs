using backend.Dtos;
using backend.Requests.Orders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var orders = await _mediator.Send(new GetOrdersQuery(pageNumber, pageSize), ct);
        return Ok(orders);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOne(Guid id, CancellationToken ct = default)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery(id), ct);
        if (order == null) return NotFound();
        return Ok(order);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest req, CancellationToken ct = default)
    {
        var order = await _mediator.Send(
            new CreateOrderCommand(req.OrderType, req.TotalAmount, req.DownloadUrl, req.ShippingAddress, req.TrackingNumber),
            ct
        );

        return Ok(order);
    }

    [Authorize]
    [HttpPost("digital")]
    public async Task<IActionResult> CreateDigital([FromBody] CreateDigitalOrderRequest req, CancellationToken ct = default)
    {
        var order = await _mediator.Send(new CreateDigitalOrderCommand(req.TotalAmount, req.DownloadUrl), ct);
        return Ok(order);
    }

    [Authorize]
    [HttpPost("physical")]
    public async Task<IActionResult> CreatePhysical([FromBody] CreatePhysicalOrderRequest req, CancellationToken ct = default)
    {
        var order = await _mediator.Send(new CreatePhysicalOrderCommand(req.TotalAmount, req.ShippingAddress, req.TrackingNumber), ct);
        return Ok(order);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrderRequest req, CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new UpdateOrderCommand(id, req.TotalAmount, req.Status, req.DownloadUrl, req.ShippingAddress, req.TrackingNumber),
            ct
        );

        if (result.NotFound) return NotFound();
        if (result.ValidationError != null) return BadRequest(result.ValidationError);
        return Ok(result.Order);
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var deleted = await _mediator.Send(new DeleteOrderCommand(id), ct);
        if (!deleted) return NotFound();
        return Ok(new { id });
    }
}
