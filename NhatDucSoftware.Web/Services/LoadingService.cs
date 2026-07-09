namespace NhatDucSoftware.Web.Services;

public class LoadingService
{
    public const string DefaultMessage = "Loading... Please Wait...";

    private int _count;
    private string _message = DefaultMessage;

    public bool IsLoading => _count > 0;

    public string Message => _message;

    public event Action? OnChange;

    public void Show(string? message = null)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            _message = message;
        }
        else if (_count == 0)
        {
            _message = DefaultMessage;
        }

        _count++;
        Notify();
    }

    public void Hide()
    {
        if (_count > 0)
        {
            _count--;
        }

        if (_count == 0)
        {
            _message = DefaultMessage;
        }

        Notify();
    }

    public async Task RunAsync(Func<Task> action, string? message = null)
    {
        Show(message);
        try
        {
            await action();
        }
        finally
        {
            Hide();
        }
    }

    public async Task<T> RunAsync<T>(Func<Task<T>> action, string? message = null)
    {
        Show(message);
        try
        {
            return await action();
        }
        finally
        {
            Hide();
        }
    }

    private void Notify() => OnChange?.Invoke();
}
