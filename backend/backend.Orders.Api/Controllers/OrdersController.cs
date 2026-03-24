using backend.Domain.Data;
using backend.Orders.Dtos;
using backend.Orders.Mappers;
using backend.Orders.Requests.Orders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Orders.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(ISender sender, ILogger<OrdersController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderViewDto>>> GetOrders(CancellationToken ct)
    {
        var result = await _sender.Send(new GetOrdersQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderViewDto>> GetOrderById(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetOrderByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:guid}/workflow")]
    public async Task<ActionResult<OrderWorkflowDto>> GetOrderWorkflow(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetOrderWorkflowQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:guid}/timeline")]
    public async Task<ActionResult<IReadOnlyList<OrderTimelineItemDto>>> GetOrderTimeline(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetOrderTimelineQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<OrderViewDto>> CreateOrder(CreateOrderCommand command, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Creating order: {OrderType}, {TotalAmount}", command.OrderType, command.TotalAmount);

            var result = await _sender.Send(command, ct);
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.Errors);
            }

            return CreatedAtAction(nameof(GetOrderById), new { id = result.Value.Id }, result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            throw;
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OrderViewDto>> UpdateOrder(Guid id, UpdateOrderCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command with { Id = id }, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteOrder(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new DeleteOrderCommand(id), ct);
        return result.Value ? NoContent() : NotFound();
    }
}
