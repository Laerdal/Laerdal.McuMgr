namespace Laerdal.McuMgr.FirmwareEraser.Contracts.Native
{
    internal interface INativeFirmwareEraserQueryableProxy
    {
        string LastFatalErrorMessage { get; }
    }
}