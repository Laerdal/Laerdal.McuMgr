package no.laerdal.mcumgr_laerdal_wrapper;

import io.runtime.mcumgr.McuMgrErrorCode;
import io.runtime.mcumgr.exception.McuMgrErrorException;
import io.runtime.mcumgr.response.HasReturnCode;

final class McuMgrLogLevelHelpers
{
    public static EAndroidLoggingLevel translateLogLevel(final int minimumNativeLogLevelNumeric)
    {
        switch (minimumNativeLogLevelNumeric)
        {
            case 0:
                return EAndroidLoggingLevel.Debug;
            case 1:
                return EAndroidLoggingLevel.Verbose;
            case 2:
                return EAndroidLoggingLevel.Info;
            case 3:
                return EAndroidLoggingLevel.Warning;
            case 4:
                return EAndroidLoggingLevel.Error;
            default:
                return EAndroidLoggingLevel.Error;
        }
    }
}
