using backend.Domain.Data;
using backend.Orders.Dtos;
using backend.Orders.Requests.Orders;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace backend.Orders.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderViewDto>> GetOrderById(Guid id, CancellationToken ct)
    {
        var order = await _sender.Send(new GetOrderByIdQuery(id), ct);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderViewDto>>> GetOrders([FromQuery] GetOrdersQuery query, CancellationToken ct)
    {
        var orders = await _sender.Send(query, ct);
        return Ok(orders);
    }

    [HttpPost("digital")]
    public async Task<ActionResult<OrderViewDto>> CreateDigitalOrder(CreateDigitalOrderCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetOrderById), new { id = result.Value.Id }, result.Value) : BadRequest(result.Errors);
    }

    [HttpPost("physical")]
    public async Task<ActionResult<OrderViewDto>> CreatePhysicalOrder(CreatePhysicalOrderCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetOrderById), new { id = result.Value.Id }, result.Value) : BadRequest(result.Errors);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<OrderViewDto>> UpdateOrder(Guid id, UpdateOrderCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(new UpdateOrderCommand(id, command.TotalAmount, command.Status, command.DownloadUrl, command.ShippingAddress, command.TrackingNumber), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new DeleteOrderCommand(id), ct);
        return result.IsSuccess ? NoContent() : NotFound();
    }

    [HttpGet("{orderId}/payment")]
    public async Task<ActionResult<OrderPaymentDetailsDto>> GetOrderPaymentDetails(Guid orderId, CancellationToken ct)
    {
        var details = await _sender.Send(new GetOrderPaymentDetailsQuery(orderId), ct);
        return details is null ? NotFound() : Ok(details);
    }

    [HttpGet("{orderId}/timeline")]
    public async Task<ActionResult<IReadOnlyList<OrderTimelineItemDto>>> GetOrderTimeline(Guid orderId, CancellationToken ct)
    {
        var timeline = await _sender.Send(new GetOrderTimelineQuery(orderId), ct);
        return timeline is null ? NotFound() : Ok(timeline);
    }

    [HttpGet("{orderId}/workflow")]
    public async Task<ActionResult<OrderWorkflowDto>> GetOrderWorkflow(Guid orderId, CancellationToken ct)
    {
        var workflow = await _sender.Send(new GetOrderWorkflowQuery(orderId), ct);
        return workflow is null ? NotFound() : Ok(workflow);
    }

    [HttpPost("{orderId}/retry-payment")]
    public async Task<ActionResult<OrderViewDto>> RetryOrderPayment(Guid orderId, CancellationToken ct)
    {
        var result = await _sender.Send(new RetryOrderPaymentCommand(orderId), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }
}
