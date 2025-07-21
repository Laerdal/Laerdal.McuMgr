using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.Common.Contracts
{
    // we need this separate interface to emit logs uniformly when p.e. the user-land event handlers throw rogue exceptions
    // have a look in EventHandlerExtensions.InvokeAllEventHandlersAndIgnoreExceptions() for more info
    internal interface ILogEmittable
    {
        void OnLogEmitted(in LogEmittedEventArgs ea);
    }
}
