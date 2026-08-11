using System.Security.Claims;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using Microsoft.AspNetCore.Http;

namespace MedSafe.Logging.MiddleWares;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;

    public AuditMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        await _next(context);

        var method = context.Request.Method;
        if ((method == "POST" || method == "PUT" || method == "DELETE")
            && context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = context.User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

            db.AuditLogs.Add(new AuditLog
            {
                UserId = userId != null ? int.Parse(userId) : null,
                UserName = userName,
                Action = $"{method} {context.Request.Path}",
                Details = $"Status: {context.Response.StatusCode}",
                IpAddress = context.Connection.RemoteIpAddress?.ToString()
            });

            await db.SaveChangesAsync();
        }
    }
}
