# MyAnimeList.Wrapper
A Windows Phone 8.1 Wrapper for MyAnimeList

__How to use the wrapper__

Just create an object from one of the services and pass the API key to the constructor :
```
AnimeService = new AnimeService("yourApiKey");
MangaService = new MangaService("yourApiKey");
AuthorizationService = new AuthorizationService("yourApiKey");
```
__Test project__

I created a project to unit test the services. You need to replace the properties in the *TestSettings* class with your credentials and your API key.

