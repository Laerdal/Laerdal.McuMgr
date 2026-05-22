using System.Text.Json.Serialization;

namespace Laerdal.McuMgr.FirmwareList.Model
{
    public record InstalledFirmwareInformation(
        [property: JsonPropertyName("version")] string Version,
        [property: JsonPropertyName("slot")] int Slot,
        [property: JsonPropertyName("active")] bool IsActive,
        [property: JsonPropertyName("bootable")] bool IsBootable,
        [property: JsonPropertyName("compressed")] bool IsCompressed,
        [property: JsonPropertyName("confirmed")] bool IsConfirmed,
        [property: JsonPropertyName("image_number")] int ImageNumber,
        [property: JsonPropertyName("permanent")] bool IsPermanent,
        [property: JsonPropertyName("pending")] bool IsPending,
        [property: JsonPropertyName("hash")] string Hash
    );
}