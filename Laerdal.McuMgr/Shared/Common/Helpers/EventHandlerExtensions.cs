using System;

namespace Laerdal.McuMgr.Common.Helpers
{
    static internal class EventHandlerExtensions
    {
        static internal void InvokeAllEventHandlersAndIgnoreExceptions<TEventArgs>(this EventHandler<TEventArgs> eventHandlers, object sender, TEventArgs args)
        {
            var delegates = eventHandlers.GetInvocationList(); //00 slow but necessary
            foreach (var d in delegates)
            {
                try
                {
                    d.DynamicInvoke(sender, args);
                }
                catch
                {
                    // ignored
                }
            }
            
            //00  dont use simple .invoke() because it doesnt cut it   we need to ensure that event handlers will be called even if one of them throws an exception
            //    this is important because the user-land event handlers are subscribed first and our library event handlers are subscribed later   if the user-land
            //    event handler throws an exception then .invoke() will stop dead on its tracks and will not call this library's event handler thus causing bugs!! 
        }
    }
}