namespace AppointMan.Application.Common.Results
{
    public class Result<T> : IResult
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public Error? Error { get; }
        public T? Value { get; }

        private Result(bool isSuccess, T? value, Error? error = null)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        public static Result<T> Success(T value) => new(true, value);
        public static Result<T> Failure(Error error) => new(false, default, error);

        public TResult Match<TResult>(
            Func<T, TResult> onSuccess,
            Func<Error, TResult> onFailure)
        {
            return IsSuccess ? onSuccess(Value!) : onFailure(Error!);
        }
    }
}