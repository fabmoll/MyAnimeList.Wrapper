using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using MyAnimeList.Wrapper.Services;
using MyAnimeList.Wrapper.ServicesContracts;

namespace MyAnimeList.Wrapper.Tests.Services
{
	[TestClass]
	public class AnimeServiceTest
	{
		private AnimeService _animeService;

		[TestInitialize]
		public void Initialize()
		{
			_animeService = new AnimeService(TestSettings.UserAgent);
		}

		[TestMethod]
		[Ignore]
		public void DeleteAnimeAsync()
		{
			var result = _animeService.DeleteAnimeAsync(TestSettings.Login, TestSettings.Password, 5525);

			Assert.IsTrue(result.Result);
		}


		[TestMethod]
		[Ignore]
		public void FindTopAnimeAsync()
		{
			var result = _animeService.FindTopAnimeAsync(0, TopAnimeType.All);

			Assert.IsTrue(result.Result.Count > 0);
		}

		[TestMethod]
		[Ignore]
		public void FindAnimeListAsync()
		{
			var result = _animeService.FindAnimeListAsync("insy");

			Assert.IsTrue(result.Result.Animes.Count > 0);
		}

		[TestMethod]
		public void GetAnimeDetailAsync()
		{
			//var result = _animeService.GetAnimeDetailAsync(TestSettings.Login, TestSettings.Password, 6707);

			//Assert.IsNotNull(result.Result);

			var result = _animeService.GetAnimeDetailAsync(TestSettings.Login, TestSettings.Password, 269);

			Assert.IsNotNull(result.Result);
		}

		[TestMethod]
		[Ignore]
		public void AddAnimeAsync()
		{
			var result = _animeService.AddAnimeAsync(TestSettings.Login, TestSettings.Password, 10793, "plan to watch", 0, 0).Result;
		}

		[TestMethod]
		public void UpdateAnimeAsync()
		{
			var result = _animeService.UpdateAnimeAsync(TestSettings.Login, TestSettings.Password, 10793, "plan to watch", 1, 5).Result;
		}

		[TestMethod]
		[Ignore]
		public void SearchAnimeAsync()
		{
			var result = _animeService.SearchAnimeAsync(TestSettings.Login, TestSettings.Password, "bleach");

			Assert.IsTrue(result.Result.Count > 0);
		}



	}
}