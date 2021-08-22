![BSPlaylistDownloaderLogo](https://i.imgur.com/L06KgsQ.png)
# Beat Saber Playlist Downloader
*Spaghetti code*

Simple Windows Forms Application to install multiple Beat Saber playlists at once.\
Code is not great, but it works.\
\
Text in logo generated on [this site](https://fontmeme.com/beat-saber-font/)\
Using [Newtonsoft.Json](https://www.newtonsoft.com/json) to parse json.

## App usage
Just open app, select game location and choose playlists you want to install.\
If Auto-Start is checked, installation will start immediately after selecting playlists.\
At this moment this works with .bplist playlists.

## Bugs
Sometimes crashes if key names in json are different than expected ("name" instead of "songName").\
\
Exception handling is really poor.\
If it shows "[E] Error while fetching data", it's probably problem with connection to beatsaver api.\
If it shows "[E] Error while downloading XXX from YYY", probably song name contains some special characters that Windows can't handle in folder names, like comas etc.\
\
Would fix it later. Maybe.

## Screenshots
![Screen 1](https://i.imgur.com/p0yESI8.png)
![Screen 2](https://i.imgur.com/gUbXlqw.png)
![Screen 3](https://i.imgur.com/ABCWFG2.png)
