using System;
using Laerdal.McuMgr.Common.Contracts;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Helpers;

namespace Laerdal.McuMgr.Common.Extensions
{
    static internal class EventFiringExtensions
    {
        static internal void InvokeAndIgnoreExceptions<TEventArgs>(this ZeroCopyEventHelpers.ZeroCopyEventHandler<TEventArgs> eventHandlers, ILogEmittable sender, in TEventArgs args)
            where TEventArgs : struct //00
        {
#if DEBUG
            ArgumentNullException.ThrowIfNull(args); //better not check this in release mode for performance reasons
            ArgumentNullException.ThrowIfNull(sender);
#endif

            try
            {
                eventHandlers?.Invoke(sender, in args);
            }
            catch
            {
                //ignored
            }

            //00  the difference between .InvokeAndIgnoreExceptions() and .InvokeAllEventHandlersAndIgnoreExceptions()
            //    is that the event-firing will halt if one of the event handlers throws an exception
        }
        
        static internal void InvokeAndIgnoreExceptions<TEventArgs>(this EventHandler<TEventArgs> eventHandlers, ILogEmittable sender, TEventArgs args) //00
        {
#if DEBUG
            ArgumentNullException.ThrowIfNull(args); //better not check this in release mode for performance reasons
            ArgumentNullException.ThrowIfNull(sender);
#endif

            try
            {
                eventHandlers?.Invoke(sender, args);
            }
            catch
            {
                //ignored
            }

            //00  the difference between .InvokeAndIgnoreExceptions() and .InvokeAllEventHandlersAndIgnoreExceptions()
            //    is that the event-firing will halt if one of the event handlers throws an exception
        }
        
        static internal void InvokeAllEventHandlersAndIgnoreExceptions<TEventArgs>(this EventHandler<TEventArgs> eventHandlers, ILogEmittable sender, TEventArgs args)
        {
#if DEBUG
            ArgumentNullException.ThrowIfNull(args); //better not check this in release mode for performance reasons
            ArgumentNullException.ThrowIfNull(sender);
#endif

            try
            {
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
                                level: ELogLevel.Error,
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
            }
            catch (Exception ex)
            {
                try
                {
                    sender.OnLogEmitted(new LogEmittedEventArgs(
                        level: ELogLevel.Error,
                        message: $"[EHE.IEHAIE.020] Internal error:\n\n{ex}",
                        category: "event-handlers",
                        resource: "events"
                    ));
                }
                catch
                {
                    //ignore any exceptions here
                }
            }

            //00  dont use simple .invoke() because it doesnt cut it   we need to ensure that event handlers will be called even if one of them throws an exception
            //    this is important because the user-land event handlers are subscribed first and our library event handlers are subscribed later   if the user-land
            //    event handler throws an exception then .invoke() will stop dead on its tracks and will not call this library's event handler thus causing bugs!! 
        }
    }
}