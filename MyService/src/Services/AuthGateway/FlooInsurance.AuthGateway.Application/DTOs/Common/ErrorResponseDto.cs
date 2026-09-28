using System.Text.Json.Serialization;

namespace FlooInsurance.AuthGateway.Application.DTOs.Common;

public class ErrorResponseDto
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, string[]>? Errors { get; set; }

    public string TraceId { get; set; } = string.Empty;

    public ErrorResponseDto() { }

    public ErrorResponseDto(int statusCode, string message, string traceId, IDictionary<string, string[]>? errors = null)
    {
        StatusCode = statusCode;
        Message = message;
        TraceId = traceId;
        Errors = errors;
    }
}
