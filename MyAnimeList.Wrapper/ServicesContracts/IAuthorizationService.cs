using System.Threading.Tasks;

namespace MyAnimeList.Wrapper.ServicesContracts
{
    public interface IAuthorizationService
    {
        Task<bool> VerifyCredentialsAsync(string login, string password);
    }
}