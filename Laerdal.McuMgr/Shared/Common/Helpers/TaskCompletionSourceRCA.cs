using System.Threading.Tasks;

namespace Laerdal.McuMgr.Common.Helpers
{
    /// <summary>Like <see cref="TaskCompletionSource{T}"/> but with <see cref="TaskCreationOptions"/>.<see cref="TaskCreationOptions.RunContinuationsAsynchronously"/> turned on by default in all constructors.</summary>
    /// <remarks>
    /// Inspired by <see href="https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/">Microsoft's recommendation on TCS</see> which strongly
    /// advises using <see cref="TaskCreationOptions"/>.<see cref="TaskCreationOptions.RunContinuationsAsynchronously"/> with <see cref="TaskCompletionSource{T}"/> to avoid exotic deadlocks in certain corner-cases.
    /// </remarks>
    internal sealed class TaskCompletionSourceRCA<T> : TaskCompletionSource<T>
    {
        /// <summary>Like <see cref="TaskCompletionSource{T}"/> but with the <see cref="TaskCreationOptions"/>.<see cref="TaskCreationOptions.RunContinuationsAsynchronously"/> turned on by default in all constructors.</summary>
        /// <remarks>
        /// Inspired by <see href="https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/">Microsoft's recommendation on TCS</see> which strongly
        /// advises using <see cref="TaskCreationOptions"/>.<see cref="TaskCreationOptions.RunContinuationsAsynchronously"/> with <see cref="TaskCompletionSource{T}"/> to avoid exotic deadlocks in certain corner-cases.
        /// </remarks>
        public TaskCompletionSourceRCA() : base(TaskCreationOptions.RunContinuationsAsynchronously)
        {
        }
        
        /// <summary>Like <see cref="TaskCompletionSource{T}"/> but with <see cref="TaskCreationOptions"/>.<see cref="TaskCreationOptions.RunContinuationsAsynchronously"/> turned on by default in all constructors.</summary>
        /// <remarks>
        /// Inspired by <see href="https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/">Microsoft's recommendation on TCS</see> which strongly
        /// advises using <see cref="TaskCreationOptions"/>.<see cref="TaskCreationOptions.RunContinuationsAsynchronously"/> with <see cref="TaskCompletionSource{T}"/> to avoid exotic deadlocks in certain corner-cases.
        /// </remarks>
        public TaskCompletionSourceRCA(object state) : base(state, TaskCreationOptions.RunContinuationsAsynchronously)
        {
        }
        
        /// <summary>Like <see cref="TaskCompletionSource{T}"/> but with <see cref="TaskCreationOptions"/>.<see cref="TaskCreationOptions.RunContinuationsAsynchronously"/> turned on by default in all constructors.</summary>
        /// <remarks>
        /// Inspired by <see href="https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/">Microsoft's recommendation on TCS</see> which strongly
        /// advises using <see cref="TaskCreationOptions"/>.<see cref="TaskCreationOptions.RunContinuationsAsynchronously"/> with <see cref="TaskCompletionSource{T}"/> to avoid exotic deadlocks in certain corner-cases.
        /// </remarks>
        public TaskCompletionSourceRCA(object state, TaskCreationOptions creationOptions) : base(state, creationOptions | TaskCreationOptions.RunContinuationsAsynchronously)
        {
        }
        
        /// <summary>Like <see cref="TaskCompletionSource{T}"/> but with <see cref="TaskCreationOptions"/>.<see cref="TaskCreationOptions.RunContinuationsAsynchronously"/> turned on by default in all constructors.</summary>
        /// <remarks>
        /// Inspired by <see href="https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/">Microsoft's recommendation on TCS</see> which strongly
        /// advises using <see cref="TaskCreationOptions"/>.<see cref="TaskCreationOptions.RunContinuationsAsynchronously"/> with <see cref="TaskCompletionSource{T}"/> to avoid exotic deadlocks in certain corner-cases.
        /// </remarks>
        public TaskCompletionSourceRCA(TaskCreationOptions creationOptions) : base(creationOptions | TaskCreationOptions.RunContinuationsAsynchronously)
        {
        }
    }
}
