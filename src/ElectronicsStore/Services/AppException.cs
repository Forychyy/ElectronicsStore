namespace ElectronicsStore.Services;

// Ошибка «для пользователя»: её текст можно сразу показать на экране.
// AppException наследуется от стандартного класса Exception.
public class AppException : Exception
{
    public AppException(string message) : base(message)
    {
    }
}
