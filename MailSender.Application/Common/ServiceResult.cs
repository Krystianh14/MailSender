namespace MailSender.Application.Common;

public class ServiceResult<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? Error { get; private set; }

    private ServiceResult(bool isSuccess, T? data, string? error)
    {
        IsSuccess = isSuccess;
        Data = data;
        Error = error;
    }

    public static ServiceResult<T> Success(T data)
    {
        return new ServiceResult<T>(true, data, null);
    }

    public static ServiceResult<T> Failure(string error)
    {
        return new ServiceResult<T>(false, default, error);
    }
}