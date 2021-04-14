

<h1 align="center">Jellyfin Youtube Metadata Plugin</h1>

<p align="center">
This plugin will download metadata about Youtube videos using a Youtube API key.

</p>

## Features
Currently this will populate the following metadata
 - Thumbnails
 - Title
 - Description
 - Release Date
 - Release Year
 - People (Uploader is used as "Director" within Jellyfin)

## Usage
Currently this plugin only works with the Movie library. Your videos must have the Youtube ID in square braces at the end of the file name. The following are valid names that will be able to be identified.

 - 20110622-Coffee_-_The_Greatest_Addiction_Ever [OTVE5iPMKLg].mp4
 - 20111130-Death_to_Pennies [y5UT04p5f7U].mp4
 - 20120529-Is_Pluto_a_planet [Z_2gbGXzFbs].mp4

Future plans to allow users to specify their own regex is planned.

## Installing from my plugin repo

Adding `https://raw.githubusercontent.com/ankenyr/jellyfin-plugin-repo/master/manifest.json` to your plugin
repositories will allow you to download and install the latest version of the plugin along with other
plugins I have made.

## Build and Installing from source

1. Clone or download this repository.
1. Ensure you have .NET Core SDK setup and installed.
1. Build plugin with following command.
    ```
    dotnet publish --configuration Release --output bin
    ```
1. Create folder named `YoutubeMetadata` in the `plugins` directory inside your Jellyfin data
   directory. You can find your directory by going to Dashboard, and noticing the Paths section.
   Mine is the root folder of the default Metadata directory.
    ```
    # mkdir <Jellyfin Data Directory>/plugins/YoutubeMetadata/

    ```
1. Place the resulting files from step 3 in the `plugins/YoutubeMetadata` folder created in step 4.
    ```
    # cp -r bin/*.dll <Jellyfin Data Directory>/plugins/YoutubeMetadata/`
    ```
1. Be sure that the plugin files are owned by your `jellyfin` user:
    ```
    # chown -R jellyfin:jellyfin /var/lib/jellyfin/plugins/YoutubeMetadata/
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
1. Navigate to the [YouTube Data API v3 Page](https://console.developers.google.com/apis/api/youtube.googleapis.com/overview).
1. Click enable then select the project you created
1. You are now able to use the YoutubeMetadata agent in your libraries. 

