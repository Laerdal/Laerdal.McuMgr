using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Laerdal.McuMgr.Common.Helpers
{
    static internal class RemoteFilePathHelpers
    {
        static public void ValidateRemoteFilePathsWithDataBytes<T>(IDictionary<string, T> remoteFilePathsWithTheirDataBytes)
        {
            remoteFilePathsWithTheirDataBytes = remoteFilePathsWithTheirDataBytes ?? throw new ArgumentNullException(nameof(remoteFilePathsWithTheirDataBytes));

            foreach (var pathAndDataBytes in remoteFilePathsWithTheirDataBytes)
            {
                ValidateRemoteFilePath(pathAndDataBytes.Key);
                
                if (pathAndDataBytes.Value is null)
                    throw new ArgumentException($"Path '{pathAndDataBytes.Key}' has its bytes set to null!");
            }
        }
        
        static internal void ValidateRemoteFilePaths(IEnumerable<string> remoteFilePaths)
        {
            remoteFilePaths = remoteFilePaths ?? throw new ArgumentNullException(nameof(remoteFilePaths));

            foreach (var path in remoteFilePaths)
            {
                ValidateRemoteFilePath(path);
            }
        }
        
        static internal void ValidateRemoteFilePath(string remoteFilePath)
        {
            if (string.IsNullOrWhiteSpace(remoteFilePath))
                throw new ArgumentException($"The {nameof(remoteFilePath)} parameter is dud!");

            remoteFilePath = remoteFilePath.Trim(); //order
            if (remoteFilePath.EndsWith("/")) //00
                throw new ArgumentException($"The given {nameof(remoteFilePath)} points to a directory not a file!");

            if (remoteFilePath.Contains('\r') || remoteFilePath.Contains('\n') || remoteFilePath.Contains('\f')) //order
                throw new ArgumentException($"The given {nameof(remoteFilePath)} contains newline characters!");

            //00  we spot this very common mistake and stop it right here    otherwise it causes a very cryptic error
        }
        
        //used by the uploader
        static public IReadOnlyDictionary<string, T> SanitizeRemoteFilePathsWithData<T>(IDictionary<string, T> remoteFilePathsWithTheirDataBytes)
        {
            remoteFilePathsWithTheirDataBytes = remoteFilePathsWithTheirDataBytes ?? throw new ArgumentNullException(nameof(remoteFilePathsWithTheirDataBytes));

            var results = new Dictionary<string, T>(remoteFilePathsWithTheirDataBytes.Count);
            foreach (var pathWithDataBytes in remoteFilePathsWithTheirDataBytes)
            {
                var sanitizedPath = SanitizeRemoteFilePath(pathWithDataBytes.Key);

                if (results.ContainsKey(sanitizedPath)) //if we detect a duplicate path we simply prefer using the latest data bytes for it
                {
                    results[sanitizedPath] = pathWithDataBytes.Value;
                    continue;
                }

                results.Add(sanitizedPath, pathWithDataBytes.Value);
            }

            return results;
        }
        
        //used by the downloader
        static internal string[] SanitizeRemoteFilePaths(IEnumerable<string> remoteFilePaths)
        {
            remoteFilePaths = remoteFilePaths ?? throw new ArgumentNullException(nameof(remoteFilePaths));
            
            var sanitizedRemoteFilesPaths = remoteFilePaths
                .Select(SanitizeRemoteFilePath)
                .GroupBy(path => path)
                .Select(group => group.First()) //unique paths only
                .ToArray();

            return sanitizedRemoteFilesPaths;
        }

        static internal string SanitizeRemoteFilePath(string remoteFilePath)
        {
            remoteFilePath = remoteFilePath?.Trim() ?? "";
            
            remoteFilePath = remoteFilePath.StartsWith('/') //10
                ? remoteFilePath
                : $"/{remoteFilePath}";

            return remoteFilePath;

            //10  the target file path must be absolute   if its not then we make it so   relative paths cause exotic errors
        }
    }
}