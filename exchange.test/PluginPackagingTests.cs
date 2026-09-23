using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace exchange.test
{
    [TestClass]
    public class PluginPackagingTests
    {
        [TestMethod]
        public void PackagingProfiles_AreExactAndDeterministic()
        {
            string repository = FindRepositoryRoot();
            string temporaryParent = Path.Combine(
                Path.GetTempPath(),
                "exchange-core-plugin-packaging-tests");
            string outputRoot = Path.Combine(temporaryParent, Guid.NewGuid().ToString("N"));

            try
            {
                RunPackager(repository, outputRoot);
                string legacyManifest = VerifyProfile(
                    outputRoot,
                    "legacy-default",
                    new[]
                    {
                        "binance.config.ini",
                        "coinbase.config.ini",
                        "exchange.binance.dll",
                        "exchange.coinbase.dll"
                    });
                string acceptanceManifest = VerifyProfile(
                    outputRoot,
                    "advanced-trade-acceptance",
                    new[]
                    {
                        "binance.config.ini",
                        "coinbase.config.ini",
                        "exchange.binance.dll",
                        "exchange.coinbase.advancedtrade.dll",
                        "exchange.coinbase.dll"
                    });

                RunPackager(repository, outputRoot);

                Assert.AreEqual(
                    legacyManifest,
                    VerifyProfile(
                        outputRoot,
                        "legacy-default",
                        new[]
                        {
                            "binance.config.ini",
                            "coinbase.config.ini",
                            "exchange.binance.dll",
                            "exchange.coinbase.dll"
                        }));
                Assert.AreEqual(
                    acceptanceManifest,
                    VerifyProfile(
                        outputRoot,
                        "advanced-trade-acceptance",
                        new[]
                        {
                            "binance.config.ini",
                            "coinbase.config.ini",
                            "exchange.binance.dll",
                            "exchange.coinbase.advancedtrade.dll",
                            "exchange.coinbase.dll"
                        }));
            }
            finally
            {
                string safeParent = Path.GetFullPath(temporaryParent)
                    .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string fullOutputRoot = Path.GetFullPath(outputRoot);
                if (fullOutputRoot.StartsWith(
                        safeParent,
                        StringComparison.OrdinalIgnoreCase) &&
                    Directory.Exists(fullOutputRoot))
                {
                    Directory.Delete(fullOutputRoot, recursive: true);
                }
            }
        }

        private static string VerifyProfile(
            string outputRoot,
            string profile,
            string[] expectedFiles)
        {
            string profilePath = Path.Combine(outputRoot, profile);
            string manifestPath = Path.Combine(profilePath, "plugin-manifest.json");
            string[] actualFiles = Directory.GetFiles(profilePath)
                .Select(Path.GetFileName)
                .Where(name => name != "plugin-manifest.json")
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            CollectionAssert.AreEqual(expectedFiles, actualFiles);

            string manifestText = File.ReadAllText(manifestPath);
            using JsonDocument manifest = JsonDocument.Parse(manifestText);
            JsonElement root = manifest.RootElement;
            Assert.AreEqual(1, root.GetProperty("schema_version").GetInt32());
            Assert.AreEqual(profile, root.GetProperty("profile").GetString());
            Assert.AreEqual("x64", root.GetProperty("platform").GetString());
            Assert.AreEqual("Release", root.GetProperty("configuration").GetString());
            Assert.AreEqual("net10.0", root.GetProperty("target_framework").GetString());

            JsonElement[] entries = root.GetProperty("files").EnumerateArray().ToArray();
            CollectionAssert.AreEqual(
                expectedFiles,
                entries.Select(entry => entry.GetProperty("name").GetString()).ToArray());
            foreach (JsonElement entry in entries)
            {
                string name = entry.GetProperty("name").GetString()!;
                string filePath = Path.Combine(profilePath, name);
                Assert.AreEqual(new FileInfo(filePath).Length, entry.GetProperty("length").GetInt64());
                Assert.AreEqual(
                    Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(filePath))).ToLowerInvariant(),
                    entry.GetProperty("sha256").GetString());
            }

            return manifestText;
        }

        private static void RunPackager(string repository, string outputRoot)
        {
            string script = Path.Combine(repository, "scripts", "package-plugins.ps1");
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                WorkingDirectory = repository,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-ExecutionPolicy");
            startInfo.ArgumentList.Add("Bypass");
            startInfo.ArgumentList.Add("-File");
            startInfo.ArgumentList.Add(script);
            startInfo.ArgumentList.Add("-Configuration");
            startInfo.ArgumentList.Add("Release");
            startInfo.ArgumentList.Add("-OutputRoot");
            startInfo.ArgumentList.Add(outputRoot);

            using Process process = Process.Start(startInfo)!;
            string standardOutput = process.StandardOutput.ReadToEnd();
            string standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();

            Assert.AreEqual(
                0,
                process.ExitCode,
                $"Packaging failed.{Environment.NewLine}{standardOutput}{Environment.NewLine}{standardError}");
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "exchange.core.sln")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Could not find the exchange.core repository root.");
        }
    }
}
