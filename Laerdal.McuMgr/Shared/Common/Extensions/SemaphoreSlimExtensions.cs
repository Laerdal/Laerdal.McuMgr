using System.Threading;

namespace Laerdal.McuMgr.Common.Extensions
{
    static internal class SemaphoreSlimExtensions
    {
        static internal int TryReleaseAll(this SemaphoreSlim semaphore)
        {
            if (semaphore == null)
                return 0;

            try
            {
                while (semaphore.CurrentCount == 0) //00 vital
                {
                    semaphore.Release();
                }

                return 0;
            }
            catch (SemaphoreFullException) // can happen if release() is called too many times
            {
                return 0;
            }

            //00   its vital to keep releasing until CurrentCount > 0 because it is conceivable that
            //     we might get multiple rogue waits on the semaphore instead of one!
        }
    }
}