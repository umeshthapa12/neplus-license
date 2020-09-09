namespace NeplusLicense.Services
{
    public interface ITokenFactory
    {
        string GenerateToken(int size = 32);
    }
}