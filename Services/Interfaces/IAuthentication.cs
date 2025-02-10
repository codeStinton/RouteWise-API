namespace RouteWise.Services.Interfaces
{
    public interface IAuthentication
    {
        Task<string> GetAccessTokenAsync();
    }
}
