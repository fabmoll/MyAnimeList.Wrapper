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

__Authentication__

Actually the authentication doesn't work anymore.  MyAnimeList changed the login page and I don't have the time to maintain the wrapper.

One thing to do is to review the *GetCookies* method.  To test if the authentication works, just use the *GetAnimeDetailAsync()* in the *AnimeServiceTest* class.

__API KEY__

To receive an API key you need to contact an admin on http//myanimelist.net (http://myanimelist.net/about.php?go=contact)
