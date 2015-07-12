using System.Collections.Generic;
using System.Threading.Tasks;
using MyAnimeList.Wrapper.Model.Anime;

namespace MyAnimeList.Wrapper.ServicesContracts
{
    public enum TopAnimeType
    {
        Tv,
        Movie,
        Ova,
        Special,
        ByPopularity,
        All
    }

    public interface IAnimeService
    {
        Task<AnimeRoot> FindAnimeListAsync(string login);
        Task<bool> AddAnimeAsync(string login, string password, int animeId, string status, int watchedEpisodes, int score);
        Task<AnimeDetail> GetAnimeDetailAsync(string login, string password, int animeId);
        Task<bool> UpdateAnimeAsync(string login, string password, int animeId, string status, int watchedEpisodes, int score);
        Task<List<AnimeDetailSearchResult>> SearchAnimeAsync(string searchCriteria);
        Task<List<AnimeDetailSearchResult>> SearchAnimeAsync(string login, string password, string searchCriteria);
        Task<List<AnimeDetail>> FindTopAnimeAsync(int pageNumber, TopAnimeType topAnimeType);
        Task<bool> DeleteAnimeAsync(string login, string password, int animeId);
    }
}