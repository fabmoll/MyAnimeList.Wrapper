[![MyAnimeList MyGet Build Status](https://www.myget.org/BuildSource/Badge/fabmoll?identifier=600666ea-48ed-43d9-9c48-38b13ae3c320)](https://www.myget.org/)

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

__API KEY__

To receive an API key you need to use the following form : https://atomiconline.wufoo.com/forms/mal-api-usage-notification/
