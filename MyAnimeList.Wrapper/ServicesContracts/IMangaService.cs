using System.Collections.Generic;
using System.Threading.Tasks;
using MyAnimeList.Wrapper.Model.Manga;

namespace MyAnimeList.Wrapper.ServicesContracts
{
    public interface IMangaService
    {
        Task<MangaRoot> FindMangaListAsync(string login);
        Task<MangaDetail> GetMangaDetailAsync(string login, string password, int mangaId);
        Task<bool> AddMangaAsync(string login, string password, int mangaId, string status, int chaptersRead, int score);
        Task<bool> UpdateMangaAsync(string login, string password, int mangaId, string status, int chaptersRead, int volume, int score);
        Task<List<MangaDetailSearchResult>> SearchMangaAsync(string searchCriteria);
        Task<List<MangaDetailSearchResult>> SearchMangaAsync(string login, string password, string searchCriteria);
        Task<bool> DeleteMangaAsync(string login, string password, int mangaId);
    }
}