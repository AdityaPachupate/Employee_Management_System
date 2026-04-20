using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.DTOs;
using System.Net.Http.Json;

namespace Shared.Logging
{

    public class CentralLogger : ILogger
    {
        private static readonly HttpClient Http = new(); // One courier for the whole app
        private readonly string _category;
        private readonly IHttpContextAccessor _access;
        private readonly string _service;
        private readonly string _url;

        public CentralLogger(string category, IHttpContextAccessor access, string service, string url)
        {
            _category = category;
            _access = access;
            _service = service;
            _url = url;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        // Only log Warnings and Errors automatically to prevent database bloat
        public bool IsEnabled(LogLevel level) => level >= LogLevel.Warning;

        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? ex, Func<TState, Exception?, string> fmt)
        {
            if (!IsEnabled(level)) return;

            var log = new LogEntryDto
            {
                ServiceName = _service,
                LogLevel = level.ToString(),
                Message = $"[{_category}] {fmt(state, ex)}",
                Exception = ex?.ToString(),
                CorrelationId = _access.HttpContext?.Request.Headers["X-Correlation-Id"].ToString(),
                UserName = _access.HttpContext?.User?.Identity?.Name,
                Timestamp = DateTime.Now
            };

            // Send in the background without blocking the app
            _ = Task.Run(() => Http.PostAsJsonAsync(_url, log));
        }
    }


    public class CentralLoggerProvider : ILoggerProvider
    {
        private readonly IHttpContextAccessor _access;
        private readonly string _service;
        private readonly string _url;

        public CentralLoggerProvider(IHttpContextAccessor access, string service, string url)
        {
            _access = access;
            _service = service;
            _url = url;
        }

        public ILogger CreateLogger(string category) => new CentralLogger(category, _access, _service, _url);
        public void Dispose() { }
    }

    public static class CentralLoggerExtensions
    {
        public static ILoggingBuilder AddCentralLogger(this ILoggingBuilder builder, string serviceName, string loggingUrl)
        {
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<ILoggerProvider>(sp =>
                new CentralLoggerProvider(sp.GetRequiredService<IHttpContextAccessor>(), serviceName, loggingUrl));
            return builder;
        }
    }
}
