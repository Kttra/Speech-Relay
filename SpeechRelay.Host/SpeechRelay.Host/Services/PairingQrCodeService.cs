using QRCoder;
using SpeechRelay.Shared.Constants;
using SpeechRelay.Shared.Models;
using System.Text.Json;

public class PairingQrCodeService : IPairingQrCodeService
{
    public byte[] Generate(string ipAddress, string token, int? port = SpeechRelayNetwork.DefaultPort)
    {
        var payloadObj = new PairingPayload
        {
            Version = 1,
            IpAddress = ipAddress,
            Port = port,
            Token = token
        };

        var payload = JsonSerializer.Serialize(payloadObj, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        using var data = QRCodeGenerator.GenerateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var qr = new PngByteQRCode(data);
        return qr.GetGraphic(10);
    }
}