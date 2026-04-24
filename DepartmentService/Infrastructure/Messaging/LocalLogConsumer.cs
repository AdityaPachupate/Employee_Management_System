using MassTransit;
using Shared.DTOs;
using System.Net.Http.Json;

namespace DepartmentService.Infrastructure.Messaging
{
    public class LocalLogConsumer : IConsumer<LogEntryDto>
    {
        private static readonly HttpClient Http = new();
        private const string LoggingUrl = "http://localhost:5089/logging/logs";

        public async Task Consume(ConsumeContext<LogEntryDto> context)
        {
            var log = context.Message;
            
            // 1. Show in Console
            Console.WriteLine($">>> [INTERNAL RABBITMQ VERIFIED] Received: {log.Message}");

            // 2. Forward to LoggingService via HTTP just so you can see it in the DB logs
            try
            {
                var verificationLog = new LogEntryDto
                {
                    ServiceName = log.ServiceName,
                    LogLevel = log.LogLevel,
                    Message = "[FROM-RABBITMQ-CONSUMER] " + log.Message,
                    CorrelationId = log.CorrelationId,
                    UserName = log.UserName,
                    Timestamp = log.Timestamp
                };

                await Http.PostAsJsonAsync(LoggingUrl, verificationLog);
            }
            catch (Exception ex)
            {
                Console.WriteLine($">>> [ERROR] Could not forward verification log: {ex.Message}");
            }
        }
    }
}
