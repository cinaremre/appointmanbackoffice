namespace Rezervise.Application.Common.Responses
{
    public class BaseResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }

        public BaseResponse(bool success, string? message = null, List<string>? errors = null)
        {
            Success = success;
            Message = message;
            Errors = errors;
        }

        public static BaseResponse Ok(string? message = null)
            => new(true, message ?? "Islem basarili.");

        public static BaseResponse Fail(string message, List<string>? errors = null)
            => new(false, message, errors);

        public static BaseResponse<T> Ok<T>(T data, string? message = null)
            => new(data, true, message ?? "Islem basarili.");

        public static BaseResponse<T> Fail<T>(string message, List<string>? errors = null)
            => new(default, false, message, errors);
    }

    public class BaseResponse<T> : BaseResponse
    {
        public T? Data { get; set; }

        public BaseResponse(T? data, bool success, string? message = null, List<string>? errors = null)
            : base(success, message, errors)
        {
            Data = data;
        }
    }
}