namespace NetClaw.Api;

public record ApiResponse<T>(bool Success, string TraceId, T Data);

public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int PageIndex,
    int PageSize,
    int TotalItems,
    int TotalPage);

public record ApiError(string? Code, string Message, IDictionary<string, IReadOnlyList<ApiFieldError>>? Details = null);

public record ApiFieldError(string? Code, string Message);

public static class ApiResults
{
    public static IResult Ok<T>(HttpContext context, T data)
        => Results.Ok(new ApiResponse<T>(true, context.TraceIdentifier, data));

    public static IResult Error(
        HttpContext context,
        int statusCode,
        string message,
        string? code = null,
        IDictionary<string, IReadOnlyList<ApiFieldError>>? details = null)
        => Results.Json(
            new { error = new ApiError(code, message, details) },
            statusCode: statusCode);
}
