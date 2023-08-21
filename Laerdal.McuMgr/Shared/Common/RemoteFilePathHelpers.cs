using System;
using System.Linq;

namespace Laerdal.McuMgr.Common
{
    static internal class RemoteFilePathHelpers
    {
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

        static internal string SanitizeRemoteFilePath(string remoteFilePath)
        {
            remoteFilePath = remoteFilePath?.Trim() ?? "";
            
            remoteFilePath = remoteFilePath.StartsWith("/") //10
                ? remoteFilePath
                : $"/{remoteFilePath}";

            return remoteFilePath;

            //10  the target file path must be absolute   if its not then we make it so   relative paths cause exotic errors
        }
    }
}