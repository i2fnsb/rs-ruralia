namespace rs_ruralia.Shared.Models;

public class Result
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public string[]? Errors { get; init; }

    public static Result Success() => new() { IsSuccess = true };
    
    public static Result Failure(string error) => new() 
    { 
        IsSuccess = false, 
        ErrorMessage = error,
        Errors = new[] { error }
    };
    
    public static Result Failure(string[] errors) => new() 
    { 
        IsSuccess = false, 
        ErrorMessage = string.Join("; ", errors),
        Errors = errors
    };
}

public class Result<T> : Result
{
    public T? Data { get; init; }

    public static Result<T> Success(T data) => new() 
    { 
        IsSuccess = true, 
        Data = data 
    };
    
    public new static Result<T> Failure(string error) => new() 
    { 
        IsSuccess = false, 
        ErrorMessage = error,
        Errors = new[] { error }
    };
    
    public new static Result<T> Failure(string[] errors) => new() 
    { 
        IsSuccess = false, 
        ErrorMessage = string.Join("; ", errors),
        Errors = errors
    };
}