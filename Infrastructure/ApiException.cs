namespace VeltrexAPI.Infrastructure;

/// <summary>Thrown for expected failures; turned into <c>{ "message": ... }</c> with the given status code.</summary>
public class ApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public static ApiException NotFound(string message) => new(StatusCodes.Status404NotFound, message);
    public static ApiException BadRequest(string message) => new(StatusCodes.Status400BadRequest, message);
    public static ApiException Conflict(string message) => new(StatusCodes.Status409Conflict, message);
}
