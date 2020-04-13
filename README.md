
<h1 align="center">Jellyfin Youtube Metadata Plugin</h1>

<p align="center">
This plugin will download metadata about Youtube videos using a Youtube API key.
</p>


## Build Process

1. Clone or download this repository.
1. Ensure you have .NET Core SDK setup and installed.
1. Build plugin with following command.
    ```sh
    dotnet publish --configuration Release --output bin
    ```
1. Create folder named `YoutubeMetadata` in the `plugin` directory within `data` directory.
1. Place the resulting file from step 3 in the `plugins/YoutubeMetadata` folder created in step 4.
1. If performed correctly you will see the plugin in the dashboard plugin page.


## Setup Plugin

1. Go to [Google's Cloud Console](https://console.cloud.google.com).
1. Create a project for this plugin.
1. Navigate to the [API Credentials Page](https://console.cloud.google.com/apis/credentials).
1. Create a new API key.
1. Go to the YoutubeMetadata Plugin page in Jellyfin.
1. Input API key into the text box for the API key and click Save.
1. Enable plugin for library by going to Admin Dashboard > Libraries, clicking on the three dots for the library and checking the bot for YoutubeMetadata.