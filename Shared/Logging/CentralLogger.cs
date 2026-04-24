using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.DTOs;
using System.Net.Http.Json;
using Shared.Utilities;
using MassTransit;

namespace Shared.Logging
{
    public class CentralLogger : ILogger
    {
        private static readonly HttpClient Http = new(); // One courier for the whole app
        private readonly string _category;
        private readonly IHttpContextAccessor _access;
        private readonly string _service;
        private readonly string _url;
        private readonly IServiceProvider _sp;

        public CentralLogger(string category, IHttpContextAccessor access, string service, string url, IServiceProvider sp)
        {
            _category = category;
            _access = access;
            _service = service;
            _url = url;
            _sp = sp;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel level)
        {
            if (_category.StartsWith("Microsoft") || 
                _category.StartsWith("MassTransit") || 
                _category.StartsWith("System"))
            {
                return level >= LogLevel.Warning;
            }

            return level >= LogLevel.Information;
        }

        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? ex, Func<TState, Exception?, string> fmt)
        {
            if (!IsEnabled(level)) return;

            var rawMsg = fmt(state, ex);
            var fullMsg = $"[{_category}] {rawMsg}";

            var log = new LogEntryDto
            {
                ServiceName = _service,
                LogLevel = level.ToString(),
                Exception = ex?.ToString(),
                CorrelationId = _access.HttpContext?.Request.Headers["X-Correlation-Id"].ToString(),
                UserName = _access.HttpContext?.User?.Identity?.Name 
                    ?? _access.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? _access.HttpContext?.User?.FindFirst("sub")?.Value,
                Timestamp = TimeHelper.GetIstNow()
            };

            // 1. HTTP Path
            _ = Task.Run(async () => {
                try {
                    var httpLog = CloneLog(log);
                    httpLog.Message = "HTTP: " + fullMsg;
                    await Http.PostAsJsonAsync($"{_url.TrimEnd('/')}/logs", httpLog);
                } catch { }
            });

            // 2. RabbitMQ Path
            if (!_category.StartsWith("MassTransit")) // Extra safety
            {
                _ = Task.Run(async () => {
                    try {
                        var bus = _sp.GetService<IBus>();
                        if (bus != null)
                        {
                            var rabbitLog = CloneLog(log);
                            rabbitLog.Message = "RabbitMQ: " + fullMsg;
                            await bus.Publish(rabbitLog);
                        }
                    } catch { }
                });
            }
        }

        private LogEntryDto CloneLog(LogEntryDto source)
        {
            return new LogEntryDto
            {
                ServiceName = source.ServiceName,
                CorrelationId = source.CorrelationId,
                LogLevel = source.LogLevel,
                Message = source.Message,
                Exception = source.Exception,
                UserName = source.UserName,
                Timestamp = source.Timestamp
            };
        }
    }

    public class CentralLoggerProvider : ILoggerProvider
    {
        private readonly IHttpContextAccessor _access;
        private readonly string _service;
        private readonly string _url;
        private readonly IServiceProvider _sp;

        public CentralLoggerProvider(IHttpContextAccessor access, string service, string url, IServiceProvider sp)
        {
            _access = access;
            _service = service;
            _url = url;
            _sp = sp;
        }

        public ILogger CreateLogger(string category) => new CentralLogger(category, _access, _service, _url, _sp);
        public void Dispose() { }
    }

    public static class CentralLoggerExtensions
    {
        public static ILoggingBuilder AddCentralLogger(this ILoggingBuilder builder, string serviceName, string loggingUrl)
        {
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<ILoggerProvider>(sp =>
            {
                var access = sp.GetRequiredService<IHttpContextAccessor>();
                return new CentralLoggerProvider(access, serviceName, loggingUrl, sp);
            });
            return builder;
        }
    }
}
