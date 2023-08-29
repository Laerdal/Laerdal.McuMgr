using System;
using System.Threading.Tasks;

namespace Laerdal.McuMgr.Common.Helpers
{
    static internal class TaskExtensions
    {
        static public async Task<T> WithTimeoutInMs<T>(this Task<T> task, int timeout)
        {
            if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
                return await task;

            throw new TimeoutException();
        }
    }
}