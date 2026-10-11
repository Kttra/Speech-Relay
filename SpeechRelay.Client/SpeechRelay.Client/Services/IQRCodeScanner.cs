using SpeechRelay.Shared.Models;
using System.Threading.Tasks;

namespace SpeechRelay.Client.Services;

public interface IQRCodeScanner
{
    /// <summary>
    /// Launch a platform camera flow to scan a QR code and return the PairingPayload if successful; otherwise null.
    /// </summary>
    Task<PairingPayload?> ScanFromCameraAsync();
}
