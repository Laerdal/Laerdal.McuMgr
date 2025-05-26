using System;

namespace Laerdal.McuMgr.Common.Helpers
{
    static public class ZeroCopyEventHelpers
    {
        /// <summary>
        /// An alternative to the standard <see cref="EventHandler{TEventArgs}"/>
        /// that allows struct-based event-arguments to be passed around without copying.
        /// Meant for performance-sensitive scenarios in which:<br/><br/>
        /// - The event-args are structs<br/>
        /// - And they take more than 32bytes of memory so spam-copying them becomes a performance issue.
        /// </summary>
        public delegate void ZeroCopyEventHandler<TEventArgs>(object sender, in TEventArgs ea) where TEventArgs : struct;
    }
}