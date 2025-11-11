using System;

namespace FocusMapApi.Models;

public class ResponseModel<T>
    where T : class
{
    public bool Success { get; set; }
    public string? Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public T? Data { get; set; }

    public static ResponseModel<T> Ok(T data, string message = "", int statusCode = 200) =>
        new()
        {
            Success = true,
            Data = data,
            Message = message,
            StatusCode = statusCode,
        };

    public static ResponseModel<T> Fail(string message, int statusCode = 400) =>
        new()
        {
            Success = false,
            Message = message,
            StatusCode = statusCode,
        };
}
