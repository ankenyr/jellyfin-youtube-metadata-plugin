using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Jellyfin.Plugin.YoutubeMetadata.Tests 
{
    
    public class YTDLIntegrationTest : IDisposable
    {
        private readonly ITestOutputHelper output;
        private readonly string _containerName = "jellyfin-integration-test-" + Guid.NewGuid().ToString("N");
        private readonly string _pluginTempDir;
        private readonly string _repoRoot;
        private string? _pluginVersion;

        public YTDLIntegrationTest(ITestOutputHelper output)
        {
            var solutionDir = FindSolutionDirectory("Jellyfin.Plugin.YoutubeMetadata.sln");
            _repoRoot = solutionDir ?? Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".."));
            _pluginTempDir = Path.Combine(Path.GetTempPath(), "jellyfin-plugin-test", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_pluginTempDir);
            this.output = output;
        }

        [Fact]
        public void IntegrationTest()
        {
            output.WriteLine("Testing");
            BuildSolution();
            CopyPluginToTempDir();

            var image = "jellyfin/jellyfin:latest";
            StartJellyfinContainer(image, _pluginTempDir, _containerName);
            

        }


        private void StartJellyfinContainer(string image, string pluginHostDir, string containerName)
        {
            // Map plugin directory into /config/plugins inside the container so Jellyfin loads it.
            // Expose port 8096 for HTTP.
            var args = $"run --rm -d --name {containerName} -p 8096:8096 -v \"{pluginHostDir}:/config/plugins:ro\" {image}";
            RunProcess("docker", args, Directory.GetCurrentDirectory());
        }
        private void BuildSolution()
        {
            // Locate the solution directory by searching up from the current directory.
            var solutionFile = "Jellyfin.Plugin.YoutubeMetadata.sln";
            var solutionDir = FindSolutionDirectory(solutionFile);
            if (solutionDir == null)
                throw new InvalidOperationException($"Could not locate {solutionFile} in this repo (cwd={Directory.GetCurrentDirectory()})");

            // Run `dotnet build` for the solution. Use the solution directory as the working directory to
            // avoid fragile relative paths that depend on how the tests are executed.
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build {solutionFile} --configuration Debug",
                WorkingDirectory = solutionDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            if (!Directory.Exists(psi.WorkingDirectory))
                throw new DirectoryNotFoundException($"Build working directory does not exist: {psi.WorkingDirectory}");
            using var p = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet build");
            p.WaitForExit();
            if (p.ExitCode != 0)
            {
                var err = p.StandardError.ReadToEnd();
                var outp = p.StandardOutput.ReadToEnd();
                throw new InvalidOperationException($"dotnet build failed (code={p.ExitCode})\nstdout:\n{outp}\nstderr:\n{err}");
            }
        }

        private static string? FindSolutionDirectory(string solutionFileName)
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, solutionFileName);
                if (File.Exists(candidate))
                    return dir.FullName;
                dir = dir.Parent;
            }

            return null;
        }
        private void CopyPluginToTempDir()
        {
            // Compiled outputs may be under the plugin project `bin/Debug/net9.0` (or other TFM/config).
            var pluginProjectDir = Path.Combine(_repoRoot, "Jellyfin.Plugin.YoutubeMetadata");
            var searchRoot = Path.Combine(pluginProjectDir, "bin");
            if (!Directory.Exists(searchRoot))
                throw new DirectoryNotFoundException("Plugin build output not found. Run build first.");

            // Try to locate the most recent build folder
            var dlls = Directory.GetFiles(searchRoot, "Jellyfin.Plugin.YoutubeMetadata.dll", SearchOption.AllDirectories);
            if (dlls.Length == 0)
                throw new FileNotFoundException("Built plugin DLL not found under bin/. Ensure the plugin was built for a supported TFM.");

            var buildDir = Path.GetDirectoryName(dlls.OrderByDescending(File.GetLastWriteTimeUtc).First()) ?? searchRoot;
            foreach (var file in Directory.GetFiles(buildDir))
            {
                var dest = Path.Combine(_pluginTempDir, Path.GetFileName(file));
                File.Copy(file, dest, overwrite: true);
            }

            // Try to read the .deps.json to determine the plugin package version.
            var depsPath = Path.Combine(buildDir, "Jellyfin.Plugin.YoutubeMetadata.deps.json");
            try
            {
                if (File.Exists(depsPath))
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(depsPath));
                    if (doc.RootElement.TryGetProperty("targets", out var targets))
                    {
                        foreach (var tfm in targets.EnumerateObject())
                        {
                            var packages = tfm.Value;
                            foreach (var pkg in packages.EnumerateObject())
                            {
                                var name = pkg.Name; // e.g. "Jellyfin.Plugin.YoutubeMetadata/1.0.3.12"
                                const string prefix = "Jellyfin.Plugin.YoutubeMetadata/";
                                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                                {
                                    _pluginVersion = name.Substring(prefix.Length);
                                    output?.WriteLine($"Detected plugin version: {_pluginVersion}");
                                    break;
                                }
                            }

                            if (!string.IsNullOrEmpty(_pluginVersion))
                                break;
                        }
                    }
                }
                else
                {
                    output?.WriteLine($"deps.json not found at {depsPath}");
                }
            }
            catch (Exception ex)
            {
                output?.WriteLine($"Failed to parse deps.json: {ex.Message}");
            }
        }
        public void Dispose()
        {
            try
            {
                StopAndRemoveContainer(_containerName);
            }
            catch
            {
                // swallow - cleanup best-effort
            }

            try
            {
                if (Directory.Exists(_pluginTempDir))
                    Directory.Delete(_pluginTempDir, recursive: true);
            }
            catch
            {
            }
        }
        private void StopAndRemoveContainer(string containerName)
        {
            // Try to stop the container (it was run with --rm so it should remove itself). If still present, force remove.
            RunProcess("docker", $"stop {containerName}", Directory.GetCurrentDirectory(), allowNonZeroExit: true);
            RunProcess("docker", $"rm -f {containerName}", Directory.GetCurrentDirectory(), allowNonZeroExit: true);
        }
        private static void RunProcess(string fileName, string arguments, string workingDirectory, bool allowNonZeroExit = false)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start {fileName} {arguments}");
            p.WaitForExit();
            var outp = p.StandardOutput.ReadToEnd();
            var err = p.StandardError.ReadToEnd();
            if (p.ExitCode != 0 && !allowNonZeroExit)
                throw new InvalidOperationException($"Process {fileName} {arguments} failed with code {p.ExitCode}\nstdout:\n{outp}\nstderr:\n{err}");
        }
    }

    
}