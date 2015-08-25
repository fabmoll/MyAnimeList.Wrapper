using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using MyAnimeList.Wrapper.Services;

namespace MyAnimeList.Wrapper.Tests.Services
{
    [TestClass]
    public class MangaServiceTest
    {
        private MangaService _animeService;

        [TestInitialize]
        public void Initialize()
        {
            _animeService = new MangaService(TestSettings.UserAgent);
        }

        [TestMethod]
        [Ignore]
        public void DeleteMangaAsync()
        {
            var result = _animeService.DeleteMangaAsync(TestSettings.Login, TestSettings.Password, 161);

            Assert.IsTrue(result.Result);
        }


        [TestMethod]
        [Ignore]
        public void FindMangaListAsync()
        {
            var result = _animeService.FindMangaListAsync("insy");

            Assert.IsTrue(result.Result.Manga.Count > 0);
        }

        [TestMethod]
        public void GetMangaDetailAsync()
        {
            var result = _animeService.GetMangaDetailAsync(TestSettings.Login, TestSettings.Password, 10269);

            Assert.IsNotNull(result.Result);
        }

        [TestMethod]
        [Ignore]
        public void AddMangaAsync()
        {
            var result = _animeService.AddMangaAsync(TestSettings.Login, TestSettings.Password, 15, "plan to read", 0, 0).Result;
        }

        [TestMethod]
        [Ignore]
        public void UpdateMangaAsync()
        {
            var result = _animeService.UpdateMangaAsync(TestSettings.Login, TestSettings.Password, 15, "plan to read", 1, 1, 5).Result;
        }

        [TestMethod]
        [Ignore]
        public void SearchMangaAsync()
        {
            var result = _animeService.SearchMangaAsync(TestSettings.Login, TestSettings.Password, "bleach");

            Assert.IsTrue(result.Result.Count > 0);
        }
    }
}