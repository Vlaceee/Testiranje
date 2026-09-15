namespace ForgeMart.Api.Infrastructure;

public sealed class ApiException(int statusCode, string title, string detail) : Exception(detail)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;

    public static ApiException BadRequest(string detail) => new(StatusCodes.Status400BadRequest, "Bad request", detail);
    public static ApiException Unauthorized(string detail) => new(StatusCodes.Status401Unauthorized, "Unauthorized", detail);
    public static ApiException Forbidden(string detail) => new(StatusCodes.Status403Forbidden, "Forbidden", detail);
    public static ApiException NotFound(string detail) => new(StatusCodes.Status404NotFound, "Not found", detail);
    public static ApiException Conflict(string detail) => new(StatusCodes.Status409Conflict, "Conflict", detail);
}

