using backend.Api.Application.Results;
using backend.Orders.Dtos;
using backend.Orders.Requests.Orders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Api.Controllers;

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
    [HttpGet("{id:guid}/payment")]
    public async Task<IActionResult> GetPayment(Guid id, CancellationToken ct = default)
    {
        var details = await _mediator.Send(new GetOrderPaymentDetailsQuery(id), ct);
        if (details == null) return NotFound();
        return Ok(details);
    }

    [Authorize]
    [HttpGet("{id:guid}/timeline")]
    public async Task<IActionResult> GetTimeline(Guid id, CancellationToken ct = default)
    {
        var timeline = await _mediator.Send(new GetOrderTimelineQuery(id), ct);
        if (timeline == null) return NotFound();
        return Ok(timeline);
    }

    [Authorize]
    [HttpGet("{id:guid}/workflow")]
    public async Task<IActionResult> GetWorkflow(Guid id, CancellationToken ct = default)
    {
        var workflow = await _mediator.Send(new GetOrderWorkflowQuery(id), ct);
        if (workflow == null) return NotFound();
        return Ok(workflow);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest req, CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new CreateOrderCommand(req.OrderType, req.TotalAmount, req.DownloadUrl, req.ShippingAddress, req.TrackingNumber),
            ct
        );

        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpPost("digital")]
    public async Task<IActionResult> CreateDigital([FromBody] CreateDigitalOrderRequest req, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CreateDigitalOrderCommand(req.TotalAmount, req.DownloadUrl), ct);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpPost("physical")]
    public async Task<IActionResult> CreatePhysical([FromBody] CreatePhysicalOrderRequest req, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CreatePhysicalOrderCommand(req.TotalAmount, req.ShippingAddress, req.TrackingNumber), ct);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpPost("{id:guid}/retry-payment")]
    public async Task<IActionResult> RetryPayment(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new RetryOrderPaymentCommand(id), ct);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrderRequest req, CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new UpdateOrderCommand(id, req.TotalAmount, req.Status, req.DownloadUrl, req.ShippingAddress, req.TrackingNumber),
            ct
        );

        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteOrderCommand(id), ct);
        return this.ToActionResult(result, _ => Ok(new { id }));
    }
}
