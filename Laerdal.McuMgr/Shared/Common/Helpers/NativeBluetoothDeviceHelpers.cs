using System;

namespace Laerdal.McuMgr.Common.Helpers
{
    internal sealed class NativeBluetoothDeviceHelpers
    {
        static internal T EnsureObjectIsCastableToType<T>(object obj, string parameterName, bool allowNulls = false) where T : class
            => obj switch
            {
                T castedNativeBluetoothDevice => castedNativeBluetoothDevice,

                null => allowNulls //context in android can be null just fine
                    ? null
                    : throw new ArgumentNullException(parameterName),

                _ => throw new ArgumentException($"Expected '{parameterName}' to be of type {typeof(T).Name} but instead it was of type {obj.GetType().Name}.", nameof(obj))
            };
    }
}
