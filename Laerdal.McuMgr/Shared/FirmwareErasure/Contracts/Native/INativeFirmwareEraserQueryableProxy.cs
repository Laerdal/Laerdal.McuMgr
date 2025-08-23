namespace Laerdal.McuMgr.FirmwareErasure.Contracts.Native
{
    internal interface INativeFirmwareEraserQueryableProxy
    {
        string LastFatalErrorMessage { get; }
    }
}