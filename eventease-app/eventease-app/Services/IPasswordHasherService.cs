namespace eventease_app.Services
{
    public interface IPasswordHasherService
    {
        string HashPassword(string password);
    }
}
