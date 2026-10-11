public interface IPairingQrCodeService
{
    byte[] Generate(string ipAddress, string token, int? port = 5000);
}