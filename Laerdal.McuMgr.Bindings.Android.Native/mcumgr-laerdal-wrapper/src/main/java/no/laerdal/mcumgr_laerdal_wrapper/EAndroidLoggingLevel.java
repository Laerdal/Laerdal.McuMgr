package no.laerdal.mcumgr_laerdal_wrapper;

import org.jetbrains.annotations.NotNull;

public enum EAndroidLoggingLevel
{
    Trace("TRACE"),
    Debug("DEBUG"),
    Verbose("VERBOSE"),
    Info("INFO"),
    Warning("WARN"),
    Error("ERROR"),
    Fatal("FATAL");

    @NotNull
    @SuppressWarnings({"FieldCanBeLocal", "unused"})
    private final String _value;

    EAndroidLoggingLevel(@NotNull final String value)
    {
        _value = value;
    }

    @Override
    public @NotNull String toString() {
        return _value;
    }
}
