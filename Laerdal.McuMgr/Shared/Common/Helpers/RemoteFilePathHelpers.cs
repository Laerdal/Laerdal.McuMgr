using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Laerdal.McuMgr.Common.Helpers
{
    static internal class RemoteFilePathHelpers
    {
        static internal string ValidateAndSanitizeRemoteFilePath(string remoteFilePath)
        {
            ValidateRemoteFilePath(remoteFilePath); //throws an exception if something is wrong

            return SanitizeRemoteFilePath(remoteFilePath);
        }

        static internal FrozenDictionary<string, T> ValidateAndSanitizeRemoteFilePathsWithData<T>(IDictionary<string, T> remoteFilePathsWithTheirDataBytes)
        {
            ValidateRemoteFilePathsWithData(remoteFilePathsWithTheirDataBytes); //throws an exception if something is wrong

            return SanitizeRemoteFilePathsWithData(remoteFilePathsWithTheirDataBytes);
        }
        
        static internal string[] ValidateAndSanitizeRemoteFilePaths(IEnumerable<string> remoteFilePaths)
        {
            ValidateRemoteFilePaths(remoteFilePaths); //throws an exception if something is wrong

            return SanitizeRemoteFilePaths(remoteFilePaths);
        }
        
        static internal void ValidateRemoteFilePathsWithData<T>(IDictionary<string, T> remoteFilePathsWithTheirData)
        {
            remoteFilePathsWithTheirData = remoteFilePathsWithTheirData ?? throw new ArgumentNullException(nameof(remoteFilePathsWithTheirData));

            foreach (var pathAndDataBytes in remoteFilePathsWithTheirData)
            {
                ValidatePayload(pathAndDataBytes.Value);
                ValidateRemoteFilePath(pathAndDataBytes.Key);
            }
        }
        
        static internal void ValidatePayload<T>(T payloadForUploading)
        {
            if (payloadForUploading == null)
                throw new ArgumentException("Bytes set to null!");
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
            if (remoteFilePath.EndsWith('/')) //00
                throw new ArgumentException($"The given {nameof(remoteFilePath)} points to a directory not a file!");

            if (remoteFilePath.Contains('\r') || remoteFilePath.Contains('\n') || remoteFilePath.Contains('\f')) //order
                throw new ArgumentException($"The given {nameof(remoteFilePath)} contains newline characters!");

            //00  we spot this very common mistake and stop it right here    otherwise it causes a very cryptic error
        }
        
        //used by the uploader
        static internal FrozenDictionary<string, T> SanitizeRemoteFilePathsWithData<T>(IDictionary<string, T> remoteFilePathsWithTheirDataBytes)
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

            return results.ToFrozenDictionary();
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