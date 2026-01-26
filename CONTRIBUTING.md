# Contributing

Thank you for contributing to Jellyfin Youtube Metadata Plugin.

## Build and install from source

1. Clone or download this repository.
1. Ensure you have .NET Core SDK set up and installed.
1. Build plugin with following command.

   ```sh
   dotnet publish --configuration Release --output bin
   ```

1. Create folder named `YoutubeMetadata` in the `plugins` directory inside your Jellyfin data
   directory. You can find your directory by going to Dashboard, and noticing the Paths section.
   Mine is the root folder of the default Metadata directory.

   ```sh
   mkdir <Jellyfin Data Directory>/plugins/YoutubeMetadata/
   ```

1. Place the resulting files from step 3 in the `plugins/YoutubeMetadata` folder created in step 4.

   ```sh
   cp -r bin/*.dll <Jellyfin Data Directory>/plugins/YoutubeMetadata/
   ```

1. Be sure that the plugin files are owned by your `jellyfin` user:

   ```sh
   chown -R jellyfin:jellyfin /var/lib/jellyfin/plugins/YoutubeMetadata/
   ```

1. If performed correctly you will see a plugin named YoutubeMetadata in `Admin -> Dashboard ->
   Advanced -> Plugins`.

## Releasing a new version

1. Update `build.yaml` with the new version number and changelog.
1. Commit the changes.
1. Create a new Release on GitHub.
1. Set the tag version. **Important**: The tag must match the version in `build.yaml` with the
   inclusion of a prefixing v. i.e. if `build.yaml` has `1.2.3.4` the tag version must be
   `v1.2.3.4`.
1. Create version release notes that match the changelog.
1. Publish the release.
1. The GitHub Action will automatically:

   - Build the plugin.
   - Verify the tag matches `build.yaml`.
   - Upload artifacts.
   - Update the repository manifest.
