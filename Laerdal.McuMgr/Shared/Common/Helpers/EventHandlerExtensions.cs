using System;
using Laerdal.McuMgr.Common.Contracts;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FirmwareInstaller.Contracts;

namespace Laerdal.McuMgr.Common.Helpers
{
    static internal class EventHandlerExtensions
    {
        static internal void InvokeAllEventHandlersAndIgnoreExceptions<TEventArgs>(this EventHandler<TEventArgs> eventHandlers, ILogEmittable sender, TEventArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            ArgumentNullException.ThrowIfNull(sender);

            if (eventHandlers == null)
                return;

            var delegates = eventHandlers.GetInvocationList(); //00 slow but necessary
            foreach (var d in delegates)
            {
                try
                {
                    d.DynamicInvoke(sender, args);
                }
                catch (Exception ex)
                {
                    var errorOccurredDuringLogging = eventHandlers is EventHandler<LogEmittedEventArgs>;
                    if (errorOccurredDuringLogging) //this means that the logging mechanism in userland is itself broken   so we dont want to risk it further by trying to log the error
                        return;

                    try
                    {
                        sender.OnLogEmitted(new LogEmittedEventArgs( //try to at least inform the calling environment that the user-land event handler messed up big time!
                            level: Enums.ELogLevel.Error,
                            message:
                            $"[EHE.IEHAIE.010] An event handler threw an exception (which got ignored) while firing event '{nameof(TEventArgs)}'" +
                            $"(This shouldn't happen! If the error stems from user-land you should fix your code!):\n\n{ex}",
                            category: "event-handlers",
                            resource: "events"
                        ));
                    }
                    catch
                    {
                        //ignore any exceptions here
                    }
                }
            }
            
            //00  dont use simple .invoke() because it doesnt cut it   we need to ensure that event handlers will be called even if one of them throws an exception
            //    this is important because the user-land event handlers are subscribed first and our library event handlers are subscribed later   if the user-land
            //    event handler throws an exception then .invoke() will stop dead on its tracks and will not call this library's event handler thus causing bugs!! 
        }
    }
}