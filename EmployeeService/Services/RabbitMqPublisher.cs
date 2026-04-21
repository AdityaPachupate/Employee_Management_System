using EmployeeService.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace EmployeeService.Services
{
    public class RabbitMqPublisher
    {
        private readonly IConnection? _connection;
        private readonly ILogger<RabbitMqPublisher> _logger;

        public RabbitMqPublisher(IConfiguration config, ILogger<RabbitMqPublisher> logger)
        {
            _logger = logger;

            var host = config["RabbitMQ:Host"];

            //RabbitMQ not configured 
            if (string.IsNullOrWhiteSpace(host))
            {
                _logger.LogWarning("RabbitMQ host not configured. Event publishing disabled.");
                return;
            }

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = host
                };

                _connection = factory.CreateConnection();
                _logger.LogInformation("RabbitMQ connection established.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ not available. Event publishing disabled.");
            }
        }

        public void PublishEmployeeCreated(Employee employee, string correlationId)
            => Publish("employee.created", employee, correlationId);

        public void PublishEmployeeUpdated(Employee employee, string correlationId)
            => Publish("employee.updated", employee, correlationId);

        public void PublishEmployeeDeleted(Employee employee, string correlationId)
            => Publish("employee.deleted", employee, correlationId);

        private void Publish(string routingKey, Employee employee, string correlationId)
        {
            if (_connection == null)
                return; // skip

            using var channel = _connection.CreateModel();
            channel.ExchangeDeclare("employee.events", ExchangeType.Topic, durable: true);

            var payload = new
            {
                employee.Id,
                employee.DepartmentId,
                employee.Salary,
                Action = routingKey,
                CorrelationId = correlationId
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

            channel.BasicPublish(
                exchange: "employee.events",
                routingKey: routingKey,
                basicProperties: null,
                body: body);
        }


    }
}
