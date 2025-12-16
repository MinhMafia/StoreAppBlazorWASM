using StoreApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Security.Claims;

namespace StoreApp.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;
        private const int MaxPayloadLength = 1000;

        public RequestLoggingMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
        {
            _next = next;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                var request = context.Request;
                var method = request.Method.ToUpper();

                // Chỉ log POST / PUT / DELETE
                if (method == "POST" || method == "PUT" || method == "DELETE")
                {
                    string bodyText = "[Empty Body]";

                    // Đọc request body nhưng KHÔNG phá stream
                    if (request.ContentLength > 0)
                    {
                        try
                        {
                            request.EnableBuffering(); // 🔥 BẮT BUỘC để JWT đọc lại được body

                            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                            bodyText = await reader.ReadToEndAsync();

                            request.Body.Position = 0; // 🔥 Reset để các middleware sau đọc được body
                        }
                        catch
                        {
                            bodyText = "[Unreadable Body]";
                        }
                    }

                    // Extract info
                    string path = request.Path.ToString();
                    string entityName = NormalizeEntityName(ExtractEntityName(path));
                    string entityId = ExtractEntityId(path);

                    string action = method switch
                    {
                        "POST" => $"CREATE_{entityName}",
                        "PUT" => $"UPDATE_{entityName}",
                        "DELETE" => $"DELETE_{entityName}",
                        _ => $"HTTP_{method}_{entityName}"
                    };

                    // Lấy IP
                    string ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    // Lấy UserId từ JWT
                    int? userId = GetUserId(context);
                    string role = GetUserRole(context);

                    // Không log nếu là customer
                    if (string.Equals(role, "customer", StringComparison.OrdinalIgnoreCase))
                    {
                        await _next(context);
                        return;
                    }

                    // Lấy Username từ JWT nếu có
                    string userName =
                        context.User?.FindFirst("username")?.Value ??
                        context.User?.FindFirst(ClaimTypes.Name)?.Value ??
                        "anonymous";

                    string payload = PreparePayload(bodyText);

                    Console.WriteLine($"[LOG MIDDLEWARE] {action} on {entityName}#{entityId}");

                    // ================================
                    // Ghi vào DB
                    // ================================
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var logService = scope.ServiceProvider.GetRequiredService<ActivityLogService>();

                        await logService.LogAsync(
                            userId ,
                            action,
                            entityName,
                            entityId,
                            payload,
                            ip
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[LOG MIDDLEWARE] DB error: {ex}");
                    }

                    // ================================
                    // Ghi ra file
                    // ================================
                    try
                    {
                        var logLine =
                            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | User:{userName} | Action:{action} | Entity:{entityName}#{entityId} | IP:{ip} | Payload:{payload}";
                        await File.AppendAllTextAsync("request_log.txt", logLine + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[LOG MIDDLEWARE] File write error: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOG MIDDLEWARE] Unexpected error: {ex}");
            }

            await _next(context);
        }

        // ================================
        //	   Helper Methods
        // ================================

        private string NormalizeEntityName(string name) =>
            name switch
            {
                "Inventoryadjustment" => "Inventory_Adjustment",
                _ => name
            };

        private string ExtractEntityName(string path)
        {
            var match = Regex.Match(path, @"^/api/([^/]+)");
            if (!match.Success) return "Unknown";
            string name = match.Groups[1].Value;
            return char.ToUpper(name[0]) + name.Substring(1).ToLower();
        }

        private string ExtractEntityId(string path)
        {
            var match = Regex.Match(path, @"^/api/[^/]+/([^/?]+)");
            return match.Success ? match.Groups[1].Value : "0";
        }

        private int? GetUserId(HttpContext context)
        {
            if (context?.User?.Identity?.IsAuthenticated == true)
            {
                var claim = context.User.FindFirst("uid")
                            ?? context.User.FindFirst(ClaimTypes.NameIdentifier);

                if (int.TryParse(claim?.Value, out int id))
                    return id;
            }

            return null;
        }


        // Lấy role
        private string? GetUserRole(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated != true)
                return null;

            return context.User.FindFirst(ClaimTypes.Role)?.Value;
        }



        private string PreparePayload(string text)
        {
            object parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<object>(text) ?? new { raw = text };
            }
            catch
            {
                parsed = new { raw = text };
            }

            string json = JsonSerializer.Serialize(parsed);

            json = FilterSensitiveFields(json);

            if (json.Length > MaxPayloadLength)
                json = json.Substring(0, MaxPayloadLength) + "...[truncated]";

            return json;
        }

        private string FilterSensitiveFields(string json)
        {
            string[] keys = { "password", "token", "creditCard" };

            foreach (var key in keys)
            {
                json = Regex.Replace(
                    json,
                    $"\"{key}\"\\s*:\\s*\".*?\"",
                    $"\"{key}\":\"[REDACTED]\"",
                    RegexOptions.IgnoreCase
                );
            }

            return json;
        }
    }
}
