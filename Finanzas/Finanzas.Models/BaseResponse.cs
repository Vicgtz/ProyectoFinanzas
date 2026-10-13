namespace Finanzas.Models;

public class BaseResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public long Id { get; set; }
}

public class BaseResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public T? Data { get; set; }
}
