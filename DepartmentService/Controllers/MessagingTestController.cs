using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;

namespace DepartmentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagingTestController : ControllerBase
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<MessagingTestController> _logger;

        public MessagingTestController(IPublishEndpoint publishEndpoint, ILogger<MessagingTestController> _logger)
        {
            _publishEndpoint = publishEndpoint;
            this._logger = _logger;
        }

        [HttpPost("simulate-create")]
        public async Task<IActionResult> SimulateCreate([FromBody] EmployeeEvent @event)
        {
            @event.EventType = EmployeeEventType.Created;
            _logger.LogInformation("Simulating CREATE event for Employee {EmployeeId} in Dept {Dept}", @event.EmployeeId, @event.DepartmentId);
            
            await _publishEndpoint.Publish(@event);

            return Ok(new { Message = "Create event published", Event = @event });
        }

        [HttpPost("simulate-update")]
        public async Task<IActionResult> SimulateUpdate([FromBody] EmployeeEvent @event)
        {
            @event.EventType = EmployeeEventType.Updated;
            _logger.LogInformation("Simulating UPDATE event for Employee {EmployeeId} in Dept {Dept}", @event.EmployeeId, @event.DepartmentId);
            
            await _publishEndpoint.Publish(@event);

            return Ok(new { Message = "Update event published", Event = @event });
        }

        [HttpPost("simulate-delete")]
        public async Task<IActionResult> SimulateDelete([FromBody] EmployeeEvent @event)
        {
            @event.EventType = EmployeeEventType.Deleted;
            _logger.LogInformation("Simulating DELETE event for Employee {EmployeeId} from Dept {Dept}", @event.EmployeeId, @event.DepartmentId);
            
            await _publishEndpoint.Publish(@event);

            return Ok(new { Message = "Delete event published", Event = @event });
        }
    }
}
