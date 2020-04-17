
<h1 align="center">Jellyfin Youtube Metadata Plugin</h1>

<p align="center">
This plugin will download metadata about Youtube videos using a Youtube API key.
</p>


## Build Process

1. Clone or download this repository.
1. Ensure you have .NET Core SDK setup and installed.
1. Build plugin with following command.
    ```
    dotnet publish --configuration Release --output bin
    ```
1. Create folder named `YoutubeMetadata` in the `plugins` directory inside your Jellyfin data
   directory, usually `/var/lib/jellyfin`. 
1. Place the resulting files from step 3 in the `plugins/YoutubeMetadata` folder created in step 4.
    ```
    $ chmod +x bin/*.dll
    # cp -r bin/*.dll /var/lib/jellyfin/plugins/YoutubeMetadata/`
    ```
1. Be sure that the plugin files are owned by your `jellyfin` user:
    ```
    # chown jellyfin:jellyfin /var/lib/jellyfin/plugins/YoutubeMetadata/*.dll
    ```
1. If performed correctly you will see a plugin named YoutubeMetadata in `Admin -> Dashboard ->
   Advanced -> Plugins`.


## Setup Plugin

1. Go to [Google's Cloud Console](https://console.cloud.google.com).
1. Create a project for this plugin.
1. Navigate to the [API Credentials Page](https://console.cloud.google.com/apis/credentials).
1. Create a new API key.
1. Go to the YoutubeMetadata Plugin page in Jellyfin.
1. Input API key into the text box for the API key and click Save.
1. You are now able to use the YoutubeMetadata agent in your libraries. 
