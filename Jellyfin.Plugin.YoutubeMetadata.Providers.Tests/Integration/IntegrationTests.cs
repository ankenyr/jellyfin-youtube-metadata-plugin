using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Threading;
using System.Text;
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
        private readonly string _pluginTempDir = string.Empty;
        private readonly string _solutionDir = string.Empty;
        private readonly string _repoRoot = string.Empty;
        private string? _pluginVersion;

        private string? _baseUrl;

        public YTDLIntegrationTest(ITestOutputHelper output)
        {
            _solutionDir = FindSolutionDirectory("Jellyfin.Plugin.YoutubeMetadata.sln") ?? throw new DirectoryNotFoundException("Solution file 'Jellyfin.Plugin.YoutubeMetadata.sln' not found in parent directories.");
            _repoRoot = _solutionDir ?? Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".."));
            // _pluginTempDir = Path.Combine(".test-artifacts", "jellyfin-plugin-test", Guid.NewGuid().ToString("N"));
            // Directory.CreateDirectory(_pluginTempDir);
            this.output = output;
            _pluginTempDir = BuildSolution();
            var image = "jellyfin/jellyfin:latest";
            StartJellyfinContainer(image, _pluginTempDir, _containerName);
            CopyBackupIntoContainerAndStartJellyfin();
        }

        [Fact]
        public async void IntegrationTest()
        {
            
            Console.WriteLine("Done");
        }

        [Fact]
        public async void IntegrationTestTwo()
        {
            
            Console.WriteLine("Done Two");
        }

        private void StartJellyfinContainer(string image, string pluginHostDir, string containerName)
        {
            // Map plugin directory into /config/plugins inside the container so Jellyfin loads it.
            // Expose port 8096 for HTTP.
            var args = $"run -d --name {containerName} -p 8096:8096 --entrypoint sleep jellyfin/jellyfin:latest infinity";
            
            RunProcess("docker", args, Directory.GetCurrentDirectory());
        }
        private string BuildSolution()
        {
            // Locate the solution directory by searching up from the current directory.
            var solutionFile = "Jellyfin.Plugin.YoutubeMetadata.sln";
            

            // Run `dotnet build` for the solution. Use the solution directory as the working directory to
            // avoid fragile relative paths that depend on how the tests are executed.
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build {solutionFile} --configuration Debug",
                WorkingDirectory = _solutionDir,
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
            return Path.Combine(".test-artifacts", "jellyfin-plugin-test", Guid.NewGuid().ToString("N"));
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
        private void CopyPluginIntoContainer()
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
                                var name = pkg.Name;
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
            var pluginPath = $"/config/plugins/YoutubeMetadata_{_pluginVersion}/";
            var args = $"exec {_containerName} mkdir -p {pluginPath}";
            RunProcess("docker", args, Directory.GetCurrentDirectory());
            args = $"cp {buildDir}/. {_containerName}:{pluginPath}";
            RunProcess("docker", args, Directory.GetCurrentDirectory());
        }

        private void CopyBackupIntoContainerAndStartJellyfin()
        {
            var integrationDir = Path.Combine(_repoRoot, "Jellyfin.Plugin.YoutubeMetadata.Providers.Tests", "Integration");
            var backupFileName = "jellyfin-backup-20251230224958.zip";
            var backupPath = Path.Combine(integrationDir, backupFileName);
            if (!File.Exists(backupPath))
                throw new FileNotFoundException($"Backup file not found: {backupPath}");

            var args = $"exec -d {_containerName} /jellyfin/jellyfin";
            RunProcess("docker", args, Directory.GetCurrentDirectory());

            var hostIp = "127.0.0.1";
            var hostPort = "8096";
            try
            {
                var inspectArgs = $"inspect --format \"{{{{.NetworkSettings.IPAddress}}}}:{{{{(index (index .NetworkSettings.Ports \\\"8096/tcp\\\") 0).HostPort}}}}\" {_containerName}";
                var mapping = RunProcessGetOutput("docker", inspectArgs, Directory.GetCurrentDirectory(), allowNonZeroExit: true).Trim();
                if (!string.IsNullOrEmpty(mapping) && mapping.Contains(':'))
                {
                    var parts = mapping.Split(':');
                    if (parts.Length == 2)
                    {
                        hostIp = parts[0];
                        hostPort = parts[1];
                        if (string.IsNullOrEmpty(hostIp) || hostIp == "0.0.0.0")
                            hostIp = "127.0.0.1";
                    }
                }
            }
            catch (Exception ex)
            {
                output?.WriteLine($"Failed to detect container host binding: {ex.Message}. Falling back to 127.0.0.1:8096");
            }
            _baseUrl = $"http://{hostIp}:{hostPort}";

            CheckHealth(_baseUrl);

            try
            {
                var killArgs = $"exec {_containerName} /bin/sh -c \"pidof jellyfin >/dev/null 2>&1 && kill -TERM $(pidof jellyfin) || (ps aux | grep '/jellyfin/jellyfin' | awk '{{{{print $2}}}}' | xargs -r kill -TERM)\"";
                RunProcess("docker", killArgs, Directory.GetCurrentDirectory(), allowNonZeroExit: true);
                // give the process a moment to exit
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                output?.WriteLine($"Failed to stop Jellyfin process inside container: {ex.Message}");
            }

            // 4) Ensure backups directory exists inside container, then copy backup into container
            args = $"exec {_containerName} mkdir -p /config/data/backups";
            RunProcess("docker", args, Directory.GetCurrentDirectory());

            args = $"cp {backupPath} {_containerName}:/config/data/backups/";
            RunProcess("docker", args, Directory.GetCurrentDirectory());

            CopyPluginIntoContainer();

            // 5) Start Jellyfin with --restore-archive
            args = $"exec -d {_containerName} /jellyfin/jellyfin --restore-archive /config/data/backups/{backupFileName}";
            RunProcess("docker", args, Directory.GetCurrentDirectory());
            CheckHealth(_baseUrl);
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

        private static string RunProcessGetOutput(string fileName, string arguments, string workingDirectory, bool allowNonZeroExit = false)
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
            var outp = p.StandardOutput.ReadToEnd();
            var err = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0 && !allowNonZeroExit)
                throw new InvalidOperationException($"Process {fileName} {arguments} failed with code {p.ExitCode}\nstdout:\n{outp}\nstderr:\n{err}");
            return outp ?? string.Empty;
        }
        
        private void CheckHealth(string url)
        {
            using var client = new HttpClient();
            var timeout = TimeSpan.FromSeconds(120);
            var retry = TimeSpan.FromSeconds(2);
            var sw = Stopwatch.StartNew();
            var healthyUrl = $"{url}/health";
            output?.WriteLine($"Polling Jellyfin healthy endpoint at {healthyUrl}");
            var healthy = false;
            while (sw.Elapsed < timeout)
            {
                try
                {
                    var resp = client.GetAsync(healthyUrl).Result;
                    if (resp.StatusCode == HttpStatusCode.OK)
                    {
                        output?.WriteLine("Jellyfin /health returned 200 OK.");
                        healthy = true;
                        break;
                    }
                    output?.WriteLine($"/health returned {(int)resp.StatusCode}. Retrying...");
                }
                catch (Exception ex)
                {
                    output?.WriteLine($"/health request failed: {ex.Message}");
                }
                Thread.Sleep(retry);
            }
            if (!healthy)
                throw new InvalidOperationException("Jellyfin did not become healthy in time (initial start).");
        }

    }
}