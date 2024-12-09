using System;
using System.Threading;
using System.Threading.Tasks;

namespace Laerdal.McuMgr.Common.Helpers
{
    /// <summary>
    /// A family of extension methods that ensure that the <see cref="TaskCompletionSource"/> is properly cleaned up in the case of a timeout either
    /// from the cancellation token or the timeout specified.<br/><br/>
    /// 
    /// Always bear in mind that if the <see cref="TaskCompletionSource"/> is never completed, its task will never complete. Even though the underlying
    /// Task is not actually in a "scheduler" (since TCS tasks are <see href="https://blog.stephencleary.com/2015/04/a-tour-of-task-part-10-promise-tasks.html">Promise Tasks</see>)
    /// never completing tasks, of any type, is <see href="https://devblogs.microsoft.com/pfxteam/dont-forget-to-complete-your-tasks/">generally considered a bug</see>.
    /// </summary>
    static internal class TaskCompletionSourceExtensions
    {
        /// <summary>
        /// Sets up a timeout-monitor on the given task. This is essentially a wrapper around <see cref="System.Threading.Tasks.Task.WaitAsync(TimeSpan)"/>
        /// with two major differences:<br/><br/>
        /// - If the timeout is zero or negative then it gets interpreted as "wait forever" and the method will just return the task itself.<br/><br/>
        /// - Most importantly, in the case of a <see cref="TimeoutException">TimeoutException</see> this method makes sure to properly cleanup
        /// (cancel) the task itself so that you won't have to (it's so easy to forget this and it's a common source of memory-leaks and zombie-promise-tasks
        /// that can cripple the system!).
        /// </summary>
        /// <param name="tcs">The task to monitor with a timeout.</param>
        /// <param name="timeoutInMilliseconds">The timeout in milliseconds. If zero or negative it gets interpreted as "wait forever" and the method will
        /// just return the task itself.</param>
        /// <param name="cancellationToken">(optional) The cancellation token that co-monitors the waiting mechanism.</param>
        /// <exception cref="TimeoutException">Thrown when the task didn't complete within the specified timeout.</exception>
        /// <returns>The hybridized task that you can await on if you have provided a positive timeout. The task itself otherwise.</returns>
        /// <code>
        /// // per https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/
        /// var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        /// 
        /// try
        /// {
        ///     PropertyChanged += MyEventHandler_;
        ///     await tcs.WaitAndFossilizeTaskOnOptionalTimeoutAsync(timeout);
        /// }
        /// finally
        /// {
        ///     PropertyChanged -= MyEventHandler_; //this is needed in the case of a timeout
        /// }
        /// 
        /// return;
        /// 
        /// void MyEventHandler_(object? _, SomeEventArgs ea_)
        /// {
        ///     try
        ///     {
        ///         // ... logic here ...
        ///             
        ///         PropertyChanged -= BindableObject_PropertyChanged_; //best to unwire the listener as soon as we can
        ///         tcs.TrySetResult();
        ///     }
        ///     catch (Exception ex)
        ///     {
        ///         PropertyChanged -= BindableObject_PropertyChanged_; //best to unwire the listener as soon as we can
        ///         tcs.TrySetException(ex); //vital    we need to ensure that the task gets completed one way or another
        ///     }
        /// }
        /// </code>
        static public Task WaitAndFossilizeTaskOnOptionalTimeoutAsync(this TaskCompletionSource tcs, long timeoutInMilliseconds, CancellationToken cancellationToken = default)
        {
            return timeoutInMilliseconds <= 0
                ? tcs.Task
                : tcs.WaitAndFossilizeTaskOnTimeoutAsync(TimeSpan.FromMilliseconds(timeoutInMilliseconds), cancellationToken);
        }

        /// <summary>
        /// Sets up a timeout-monitor on the given task. This is essentially a wrapper around <see cref="System.Threading.Tasks.Task.WaitAsync(TimeSpan)"/>
        /// with two major differences:<br/><br/>
        /// - If the timeout is zero or negative then it gets interpreted as "wait forever" and the method will just return the task itself.<br/><br/>
        /// - Most importantly, in the case of a <see cref="TimeoutException">TimeoutException</see> this method makes sure to properly cleanup
        /// (cancel) the task itself so that you won't have to (it's so easy to forget this and it's a common source of memory-leaks and zombie-promise-tasks
        /// that can cripple the system!)
        /// </summary>
        /// <param name="tcs">The task to monitor with a timeout.</param>
        /// <param name="timespan">The amount of time to wait for before throwing a <see cref="TimeoutException"/>. If zero or negative it gets interpreted
        /// as "wait forever" and the method will just return the task itself.</param>
        /// <param name="cancellationToken">(optional) The cancellation token that co-monitors the waiting mechanism.</param>
        /// <exception cref="TimeoutException">Thrown when the task didn't complete within the specified timeout.</exception>
        /// <returns>The hybridized task that you can await on if you have provided a positive timeout. The task itself otherwise.</returns>
        /// <code>
        /// // per https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/
        /// var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        /// 
        /// try
        /// {
        ///     PropertyChanged += MyEventHandler_;
        ///     await tcs.WaitAndFossilizeTaskOnOptionalTimeoutAsync(timeout);
        /// }
        /// finally
        /// {
        ///     PropertyChanged -= MyEventHandler_; //this is needed in the case of a timeout
        /// }
        /// 
        /// return;
        /// 
        /// void MyEventHandler_(object? _, SomeEventArgs ea_)
        /// {
        ///     try
        ///     {
        ///         // ... logic here ...
        ///             
        ///         PropertyChanged -= BindableObject_PropertyChanged_; //best to unwire the listener as soon as we can
        ///         tcs.TrySetResult();
        ///     }
        ///     catch (Exception ex)
        ///     {
        ///         PropertyChanged -= BindableObject_PropertyChanged_; //best to unwire the listener as soon as we can
        ///         tcs.TrySetException(ex); //vital    we need to ensure that the task gets completed one way or another
        ///     }
        /// }
        /// </code>
        static public Task WaitAndFossilizeTaskOnOptionalTimeoutAsync(this TaskCompletionSource tcs, TimeSpan timespan, CancellationToken cancellationToken = default)
        {
            return timespan <= TimeSpan.Zero
                ? tcs.Task
                : tcs.WaitAndFossilizeTaskOnTimeoutAsync(timespan, cancellationToken);
        }

        /// <summary>
        /// Sets up a timeout-monitor on the given task. This is essentially a wrapper around <see cref="System.Threading.Tasks.Task.WaitAsync(TimeSpan)"/>
        /// with two major differences:<br/><br/>
        /// - If the timeout is negative you will get an <see cref="ArgumentException"/> thrown (WaitAsync() allows -1 as a special-case for "wait forever"
        /// but this method doesn't allow that special case)<br/><br/>
        /// - Most importantly, in the case of a <see cref="TimeoutException">TimeoutException</see> this method makes sure to properly cleanup
        /// (cancel) the task itself so that you won't have to (it's so easy to forget this and it's a common source of memory-leaks and zombie-promise-tasks
        /// that can cripple the system!).
        /// </summary>
        /// <param name="tcs">The task to monitor with a timeout.</param>
        /// <param name="timeoutInMilliseconds">The timeout in milliseconds. If zero or negative it gets interpreted as "wait forever" and the method will
        ///     just return the task itself.</param>
        /// <param name="cancellationToken">(optional) The cancellation token that co-monitors the waiting mechanism.</param>
        /// <exception cref="TimeoutException">Thrown when the task didn't complete within the specified timeout.</exception>
        /// <returns>The hybridized task that you can await on.</returns>
        /// <code>
        /// // per https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/
        /// var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        /// 
        /// try
        /// {
        ///     PropertyChanged += MyEventHandler_;
        ///     await tcs.WaitAndFossilizeTaskOnTimeoutAsync(timeout);
        /// }
        /// finally
        /// {
        ///     PropertyChanged -= MyEventHandler_; //this is needed in the case of a timeout
        /// }
        /// 
        /// return;
        /// 
        /// void MyEventHandler_(object? _, SomeEventArgs ea_)
        /// {
        ///     try
        ///     {
        ///         // ... logic here ...
        ///             
        ///         PropertyChanged -= BindableObject_PropertyChanged_; //best to unwire the listener as soon as we can
        ///         tcs.TrySetResult();
        ///     }
        ///     catch (Exception ex)
        ///     {
        ///         PropertyChanged -= BindableObject_PropertyChanged_; //best to unwire the listener as soon as we can
        ///         tcs.TrySetException(ex); //vital    we need to ensure that the task gets completed one way or another
        ///     }
        /// }
        /// </code>
        static public Task WaitAndFossilizeTaskOnTimeoutAsync(this TaskCompletionSource tcs, long timeoutInMilliseconds, CancellationToken cancellationToken = default)
        {
            return tcs.WaitAndFossilizeTaskOnTimeoutAsync(TimeSpan.FromMilliseconds(timeoutInMilliseconds), cancellationToken);
        }

        /// <summary>
        /// Sets up a timeout-monitor on the given task. This is essentially a wrapper around <see cref="System.Threading.Tasks.Task.WaitAsync(TimeSpan)"/>
        /// with two major differences:<br/><br/>
        /// - If the timeout is negative you will get an <see cref="ArgumentException"/> thrown (WaitAsync() allows -1 as a special-case for "wait forever"
        /// but this method doesn't allow that special case)<br/><br/>
        /// - Most importantly, in the case of a <see cref="TimeoutException">TimeoutException</see> this method makes sure to properly cleanup
        /// (cancel) the task itself so that you won't have to (it's so easy to forget this and it's a common source of memory-leaks and zombie-promise-tasks
        /// that can cripple the system!).
        /// </summary>
        /// <param name="tcs">The task to monitor with a timeout.</param>
        /// <param name="timespan">The amount of time to wait for before throwing a <see cref="TimeoutException"/>. If zero or negative it gets interpreted
        ///     as "wait forever" and the method will just return the task itself.</param>
        /// <param name="cancellationToken">(optional) The cancellation token that co-monitors the waiting mechanism.</param>
        /// <exception cref="TimeoutException">Thrown when the task didn't complete within the specified timeout.</exception>
        /// <returns>The hybridized task that you can await on.</returns>
        /// <code>
        /// // per https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/
        /// var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        /// 
        /// try
        /// {
        ///     PropertyChanged += MyEventHandler_;
        ///     await tcs.WaitAndFossilizeTaskOnTimeoutAsync(timeout);
        /// }
        /// finally
        /// {
        ///     PropertyChanged -= MyEventHandler_; //this is needed in the case of a timeout
        /// }
        /// 
        /// return;
        /// 
        /// void MyEventHandler_(object? _, SomeEventArgs ea_)
        /// {
        ///     try
        ///     {
        ///         // ... logic here ...
        ///             
        ///         PropertyChanged -= BindableObject_PropertyChanged_; //best to unwire the listener as soon as we can
        ///         tcs.TrySetResult();
        ///     }
        ///     catch (Exception ex)
        ///     {
        ///         PropertyChanged -= BindableObject_PropertyChanged_; //best to unwire the listener as soon as we can
        ///         tcs.TrySetException(ex); //vital    we need to ensure that the task gets completed one way or another
        ///     }
        /// }
        /// </code>
        static public async Task WaitAndFossilizeTaskOnTimeoutAsync(this TaskCompletionSource tcs, TimeSpan timespan, CancellationToken cancellationToken = default)
        {
            if (timespan < TimeSpan.Zero) //note that this deviates from the behaviour of .WaitAsync() which does accept -1 milliseconds which means "wait forever"
            {
                var exception = new ArgumentException("Timeout must be zero or positive", nameof(timespan));
                tcs.TrySetException(exception); //vital    we need to ensure that the task gets completed one way or another

                throw exception;
            }

            try
            {
                await tcs.Task.WaitAsync(timespan, cancellationToken);
            }
            catch (Exception ex) when (ex is TimeoutException or OperationCanceledException or TaskCanceledException) //taskcanceledexception and operationcanceledexception can come from cancellation-token timeouts
            {
                var properExceptionToThrow = DeduceProperExceptionToThrow(ex, cancellationToken);
                var isFossilizationSuccessful = properExceptionToThrow is TimeoutException
                    ? tcs.TrySetException(properExceptionToThrow) //00 vital
                    : tcs.TrySetCanceled(cancellationToken); //00 vital
                if (isFossilizationSuccessful)
                {
                    if (properExceptionToThrow == ex)
                        throw; //if possible we want to throw the original exception to preserve the original stack-trace

                    throw properExceptionToThrow;
                }

                if (tcs.Task.IsCompletedSuccessfully) //10 barely completed in time
                    return; //micro-optimization to avoid the overhead of await

                await tcs.Task;
            }

            //00  it is absolutely vital to trash the tcs and ensure it will not pester the system as a zombie promise-task
            //    waitasync() does not take care of this aspect because quite simply it cannot even if it tried
            //    all waitasync() does is to simply unwire the continuation from task.Task and leave it be
            //
            //10  if the cancellation fails it means one of the following two things:
            //
            //    1. that the task barely managed to complete in time on its own at the nick of time   we prefer to give
            //       it the benefit of the doubt and let it complete normally
            //
            //    2. that the task itself threw a timeout-exception and in this case we prefer to honor the exception that
            //       the task itself threw and let it be propagated to the caller
        }

        /// <summary>
        /// Sets up a timeout-monitor on the given task. This is essentially a wrapper around <see cref="System.Threading.Tasks.Task.WaitAsync(TimeSpan)"/>
        /// with two major differences:<br/><br/>
        /// - If the timeout is zero or negative then it gets interpreted as "wait forever" and the method will just return the task itself.<br/><br/>
        /// - Most importantly, in the case of a <see cref="TimeoutException">TimeoutException</see> this method makes sure to properly cleanup
        /// (cancel) the task itself so that you won't have to (it's so easy to forget this and it's a common source of memory-leaks and zombie-promise-tasks
        /// that can cripple the system!).
        /// </summary>
        /// <param name="tcs">The task to monitor with a timeout.</param>
        /// <param name="timeoutInMilliseconds">The timeout in milliseconds. If zero or negative it gets interpreted as "wait forever" and the method
        /// will just return the task itself.</param>
        /// <param name="cancellationToken">(optional) The cancellation token that co-monitors the waiting mechanism.</param>
        /// <exception cref="TimeoutException">Thrown when the task didn't complete within the specified timeout.</exception>
        /// <returns>The hybridized task that you can await on if you have provided a positive timeout. The task itself otherwise.</returns>
        /// <code>
        /// // per https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/
        /// var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        /// 
        /// try
        /// {
        ///     PropertyChanged += MyEventHandler_;
        ///     await tcs.WaitAndFossilizeTaskOnOptionalTimeoutAsync&lt;int&gt;(timeout);
        /// }
        /// finally
        /// {
        ///     PropertyChanged -= MyEventHandler_; //this is needed in the case of a timeout
        /// }
        /// 
        /// return;
        /// 
        /// void MyEventHandler_(object? _, SomeEventArgs ea_)
        /// {
        ///     try
        ///     {
        ///         // ... logic here ...
        ///             
        ///         PropertyChanged -= BindableObject_PropertyChanged_; //best to unwire the listener as soon as we can
        ///         tcs.TrySetResult(123);
        ///     }
        ///     catch (Exception ex)
        ///     {
        ///         PropertyChanged -= BindableObject_PropertyChanged_; //best to unwire the listener as soon as we can
        ///         tcs.TrySetException(ex); //vital    we need to ensure that the task gets completed one way or another
        ///     }
        /// }
        /// </code>
        static public Task<T> WaitAndFossilizeTaskOnOptionalTimeoutAsync<T>(this TaskCompletionSource<T> tcs, long timeoutInMilliseconds, CancellationToken cancellationToken = default)
        {
            return timeoutInMilliseconds <= 0
                ? tcs.Task
                : tcs.WaitAndFossilizeTaskOnTimeoutAsync(TimeSpan.FromMilliseconds(timeoutInMilliseconds), cancellationToken);
        }

        /// <summary>
        /// Sets up a timeout-monitor on the given task. This is essentially a wrapper around <see cref="System.Threading.Tasks.Task.WaitAsync(TimeSpan)"/>
        /// with two major differences:<br/><br/>
        /// - If the timeout is zero or negative then it gets interpreted as "wait forever" and the method will just return the task itself.<br/><br/>
        /// - Most importantly, in the case of a <see cref="TimeoutException">TimeoutException</see> this method makes sure to properly cleanup
        /// (cancel) the task itself so that you won't have to (it's so easy to forget this and it's a common source of memory-leaks and zombie-promise-tasks
        /// that can cripple the system!).
        /// </summary>
        /// <param name="tcs">The task to monitor with a timeout.</param>
        /// <param name="timespan">The amount of time to wait for before throwing a <see cref="TimeoutException"/>. If zero or negative it gets interpreted
        /// as "wait forever" and the method will just return the task itself.</param>
        /// <param name="cancellationToken">(optional) The cancellation token that co-monitors the waiting mechanism.</param>
        /// <exception cref="TimeoutException">Thrown when the task didn't complete within the specified timeout.</exception>
        /// <returns>The hybridized task that you can await on if you have provided a positive timeout. The task itself otherwise.</returns>
        /// <code>
        /// // per https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/
        /// var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        /// 
        /// try
        /// {
        ///     PropertyChanged += MyEventHandler_;
        ///     await tcs.WaitAndFossilizeTaskOnOptionalTimeoutAsync&lt;int&gt;(timeout);
        /// }
        /// finally
        /// {
        ///     PropertyChanged -= MyEventHandler_; //this is needed in the case of a timeout
        /// }
        /// 
        /// return;
        /// 
        /// void MyEventHandler_(object? _, SomeEventArgs ea_)
        /// {
        ///     try
        ///     {
        ///         // ... logic here ...
        ///             
        ///         PropertyChanged -= BindableObject_PropertyChanged_; //best to unwire the listener as soon as we can
        ///         tcs.TrySetResult(123);
        ///     }
        ///     catch (Exception ex)
        ///     {
        ///         PropertyChanged -= BindableObject_PropertyChanged_; //best to unwire the listener as soon as we can
        ///         tcs.TrySetException(ex); //vital    we need to ensure that the task gets completed one way or another
        ///     }
        /// }
        /// </code>
        static public Task<T> WaitAndFossilizeTaskOnOptionalTimeoutAsync<T>(this TaskCompletionSource<T> tcs, TimeSpan timespan, CancellationToken cancellationToken = default)
        {
            return timespan <= TimeSpan.Zero
                ? tcs.Task
                : tcs.WaitAndFossilizeTaskOnTimeoutAsync(timespan, cancellationToken);
        }

        /// <summary>
        /// Sets up a timeout-monitor on the given task. This is essentially a wrapper around <see cref="System.Threading.Tasks.Task.WaitAsync(TimeSpan)"/>
        /// with two major differences:<br/><br/>
        /// - If the timeout is negative you will get an <see cref="ArgumentException"/> thrown (WaitAsync() allows -1 as a special-case for "wait forever"
        /// but this method doesn't allow that special case)<br/><br/>
        /// - Most importantly, in the case of a <see cref="TimeoutException">TimeoutException</see> this method makes sure to properly cleanup
        /// (cancel) the task itself so that you won't have to (it's so easy to forget this and it's a common source of memory-leaks and zombie-promise-tasks
        /// that can cripple the system!).
        /// </summary>
        /// <param name="tcs">The task to monitor with a timeout.</param>
        /// <param name="timeoutInMilliseconds">The timeout in milliseconds. If zero or negative it gets interpreted as "wait forever" and the method will
        /// just return the task itself.</param>
        /// <param name="cancellationToken">(optional) The cancellation token that co-monitors the waiting mechanism.</param>
        /// <exception cref="TimeoutException">Thrown when the task didn't complete within the specified timeout.</exception>
        /// <code>
        /// // per https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/
        /// var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        /// 
        /// try
        /// {
        ///     PropertyChanged += MyEventHandler_;
        ///     await tcs.WaitAndFossilizeTaskOnTimeoutAsync&lt;int&gt;(timeout);
        /// }
        /// finally
        /// {
        ///     PropertyChanged -= MyEventHandler_; //this is needed in the case of a timeout
        /// }
        /// 
        /// return;
        /// 
        /// void MyEventHandler_(object? _, SomeEventArgs ea_)
        /// {
        ///     try
        ///     {
        ///         // ... logic here ...
        ///             
        ///         PropertyChanged -= BindableObject_PropertyChanged_; //best to unwire the listener as soon as we can
        ///         tcs.TrySetResult(123);
        ///     }
        ///     catch (Exception ex)
        ///     {
        ///         PropertyChanged -= BindableObject_PropertyChanged_; //best to unwire the listener as soon as we can
        ///         tcs.TrySetException(ex); //vital    we need to ensure that the task gets completed one way or another
        ///     }
        /// }
        /// </code>
        static public Task<T> WaitAndFossilizeTaskOnTimeoutAsync<T>(this TaskCompletionSource<T> tcs, long timeoutInMilliseconds, CancellationToken cancellationToken = default)
        {
            return tcs.WaitAndFossilizeTaskOnTimeoutAsync(TimeSpan.FromMilliseconds(timeoutInMilliseconds), cancellationToken);
        }

        /// <summary>
        /// Sets up a timeout-monitor on the given task. This is essentially a wrapper around <see cref="System.Threading.Tasks.Task.WaitAsync(TimeSpan)"/>
        /// with two major differences:<br/><br/>
        /// - If the timeout is negative you will get an <see cref="ArgumentException"/> thrown (WaitAsync() allows -1 as a special-case for "wait forever"
        /// but this method doesn't allow that special case)<br/><br/>
        /// - Most importantly, in the case of a <see cref="TimeoutException">TimeoutException</see> this method makes sure to properly cleanup
        /// (cancel) the task itself so that you won't have to (it's so easy to forget this and it's a common source of memory-leaks and zombie-promise-tasks
        /// that can cripple the system!).
        /// </summary>
        /// <param name="tcs">The task to monitor with a timeout.</param>
        /// <param name="timespan">The amount of time to wait for before throwing a <see cref="TimeoutException"/>. If zero or negative it gets interpreted
        ///     as "wait forever" and the method will just return the task itself.</param>
        /// <param name="cancellationToken">(optional) The cancellation token that co-monitors the waiting mechanism.</param>
        /// <exception cref="TimeoutException">Thrown when the task didn't complete within the specified timeout.</exception>
        /// <code>
        /// // per https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/
        /// var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        /// 
        /// try
        /// {
        ///     PropertyChanged += MyEventHandler_;
        ///     await tcs.WaitAndFossilizeTaskOnTimeoutAsync&lt;int&gt;(timeout);
        /// }
        /// finally
        /// {
        ///     PropertyChanged -= MyEventHandler_; //this is needed in the case of a timeout
        /// }
        /// 
        /// return;
        /// 
        /// void MyEventHandler_(object? _, SomeEventArgs ea_)
        /// {
        ///     try
        ///     {
        ///         // ... logic here ...
        ///             
        ///         PropertyChanged -= BindableObject_PropertyChanged_; //best to unwire the listener as soon as we can
        ///         tcs.TrySetResult(123);
        ///     }
        ///     catch (Exception ex)
        ///     {
        ///         PropertyChanged -= BindableObject_PropertyChanged_; //best to unwire the listener as soon as we can
        ///         tcs.TrySetException(ex); //vital    we need to ensure that the task gets completed one way or another
        ///     }
        /// }
        /// </code>
        static public async Task<T> WaitAndFossilizeTaskOnTimeoutAsync<T>(this TaskCompletionSource<T> tcs, TimeSpan timespan, CancellationToken cancellationToken = default)
        {
            if (timespan < TimeSpan.Zero) //note that this deviates from the behaviour of .WaitAsync() which does accept -1 milliseconds which means "wait forever"
            {
                var exception = new ArgumentException("Timeout must be zero or positive", nameof(timespan));
                tcs.TrySetException(exception); //vital    we need to ensure that the task gets completed one way or another

                throw exception;
            }

            try
            {
                return await tcs.Task.WaitAsync(timespan, cancellationToken);
            }
            catch (Exception ex) when (ex is TimeoutException or OperationCanceledException or TaskCanceledException) //taskcanceledexception and operationcanceledexception can come from cancellation-token timeouts
            {
                var properExceptionToThrow = DeduceProperExceptionToThrow(ex, cancellationToken);
                var isFossilizationSuccessful = properExceptionToThrow is TimeoutException
                    ? tcs.TrySetException(properExceptionToThrow) //00 vital
                    : tcs.TrySetCanceled(cancellationToken); //00 vital
                if (isFossilizationSuccessful)
                {
                    if (properExceptionToThrow == ex)
                        throw; //if possible we want to throw the original exception to preserve the original stack-trace

                    throw properExceptionToThrow;
                }

                return tcs.Task.IsCompletedSuccessfully //10 barely completed in time
                    ? tcs.Task.Result //micro-optimization to avoid the overhead of await
                    : await tcs.Task; //this means the task itself faulted
            }

            //00  it is absolutely vital to trash the tcs and ensure it will not pester the system as a zombie promise-task
            //    waitasync() does not take care of this aspect because quite simply it cannot even if it tried
            //    all waitasync() does is to simply unwire the continuation from task.Task and leave it be
            //
            //10  if the cancellation fails it means one of the following two things:
            //
            //    1. that the task barely managed to complete in time on its own at the nick of time   we prefer to give
            //       it the benefit of the doubt and let it complete normally
            //
            //    2. that the task itself threw a timeout-exception and in this case we prefer to honor the exception that
            //       the task itself threw and let it be propagated to the caller
        }

        static private Exception DeduceProperExceptionToThrow(Exception ex, CancellationToken cancellationToken)
        {
            var exceptionToThrow = ex switch
            {
                TimeoutException => ex,

                OperationCanceledException ocex => //00
                    cancellationToken.IsCancellationRequested //10  known small bug here
                        ? ex
                        : new TimeoutException(message: "Operation timed out", innerException: ocex),

                _ => ex,
            };

            return exceptionToThrow;

            //00  bear in mind that TaskCanceledException derives from OperationCanceledException
            //
            //10  todo:  unfortunately the cancellation token does not allow us to differentiate between a timeout and a cancellation and we thus have a small bug here due
            //    todo:  to this: we throw an OperationCanceledException in the case of a timeout instead of a TimeoutException   there is a feature request about this
            //                                       https://github.com/dotnet/runtime/discussions/110461   and   https://github.com/dotnet/runtime/issues/109246 
        }
    }
}