using SpeechRelay.Shared.Models;
using System.Threading.Tasks;

namespace SpeechRelay.Client.Services;

public class QRCodeScanner : IQRCodeScanner
{
    public Task<PairingPayload?> ScanFromCameraAsync()
    {
        return Task.FromResult<PairingPayload?>(null);
    }
}
