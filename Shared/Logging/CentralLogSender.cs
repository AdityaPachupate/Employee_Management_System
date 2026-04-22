using Microsoft.AspNetCore.Http;
using Shared.DTOs;
using System.Net.Http.Json;

namespace Shared.Logging
{
    public class CentralLogSender : ILogSender
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _serviceName;

        public CentralLogSender(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, string serviceName)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _serviceName = serviceName;
        }

        public Task SendLogAsync(LogEntryDto log)
        {
            // Auto-populate if missing or empty
            if (string.IsNullOrWhiteSpace(log.ServiceName)) 
                log.ServiceName = _serviceName;

            log.Timestamp = log.Timestamp == default 
                ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")) 
                : log.Timestamp;

            // Use existing correlation ID or create a new one for this request
            log.CorrelationId ??= _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-Id"].ToString();
            if (string.IsNullOrWhiteSpace(log.CorrelationId))
                log.CorrelationId = _httpContextAccessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();

            log.UserName ??= _httpContextAccessor.HttpContext?.User?.Identity?.Name;

            try
            {
                // Send in the background so the API stays fast
                _ = Task.Run(async () => {
                    try {
                        var response = await _httpClient.PostAsJsonAsync("/logs", log);
                        if (!response.IsSuccessStatusCode) {
                             Console.WriteLine($"[LOG ERROR] Logging Service returned {response.StatusCode} for: {log.Message}");
                        }
                    } catch (Exception ex) {
                         Console.WriteLine($"[LOG FALLBACK] Could not reach Logging Service. Log: {log.Message}. Error: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOG FALLBACK] Internal error in Log Sender: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        public Task SendLogAsync(string message, string logLevel = "Information", string? exception = null)
        {
            var log = new LogEntryDto
            {
                Message = message,
                LogLevel = logLevel,
                Exception = exception
            };
            return SendLogAsync(log);
        }
    }
}
