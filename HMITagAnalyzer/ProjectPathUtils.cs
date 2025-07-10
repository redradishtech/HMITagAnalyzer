using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace HMITagAnalyzer
{
    internal static class ProjectPathUtils
    {
        private static string? GetCompanyProjectPath()
        {
            string[] drives = ["Z", "G", "C"];
            foreach (var drive in drives)
            {
                var path = $@"{drive}:\Shared drives\Projects";
                if (Directory.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        public static string GetLatestProjectDirectory()
        {
            var path = GetCompanyProjectPath();
            if (path != null)
            {
                var mostRecentDir = new DirectoryInfo(path)
                    .GetDirectories()
                    .OrderByDescending(d => d.LastWriteTime)
                    .FirstOrDefault();
                if (mostRecentDir != null)
                {
                    var subdir = mostRecentDir.FullName + @"\SCADA\configuration_files";
                    if (Directory.Exists(subdir)) return subdir;
                    else
                        return mostRecentDir.FullName;
                }
            }

            return @"C:\";
        }

        // ReSharper disable once InconsistentNaming
        public static void GetAllProjectHPRBs(string[] args)
        {
            string basePath = GetLatestProjectDirectory();
            string pattern = @"**/SCADA/configuration_files/**/*.hprb";

            // Match files using FileSystemGlobbing
            var matcher = new Matcher();
            matcher.AddInclude(pattern);

            var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(basePath)));

            // Print matched files
            foreach (var file in result.Files)
            {
                Console.WriteLine(Path.Combine(basePath, file.Path));
            }
        }
    }
}