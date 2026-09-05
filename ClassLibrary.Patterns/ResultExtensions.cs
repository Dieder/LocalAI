using System;
using System.Collections.Generic;
using System.Text;

namespace ClassLibrary.Patterns
{
    public static class ResultExtensions
    {
        public static Result<U> Map<T, U>(this Result<T> result, Func<T, U> mapper)
            => result.IsSuccess
                ? Result<U>.Success(mapper(result.Value!))
                : Result<U>.Failure(result.Error!);

        public static Result<U> OnSuccess<T, U>(this Result<T> result, Func<T, Result<U>> next)
            => result.IsSuccess ? next(result.Value!) : Result<U>.Failure(result.Error!);

        public static Result<T> OnFailure<T>(this Result<T> result, Action<string> action)
        {
            if (!result.IsSuccess)
                action(result.Error!);
            return result;
        }
    }
}
