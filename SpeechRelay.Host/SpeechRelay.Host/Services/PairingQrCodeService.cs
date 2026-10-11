using QRCoder;
using System.Text.Json;

public class PairingQrCodeService : IPairingQrCodeService
{
    public byte[] Generate(string ipAddress, string token, int? port = 5000)
    {
        var payload = JsonSerializer.Serialize(new
        {
            version = 1,
            ipAddress,
            port = port,
            token
        });

        using var data = QRCodeGenerator.GenerateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var qr = new PngByteQRCode(data);
        return qr.GetGraphic(10);
    }
}