using SpeechRelay.Shared.Constants;

public interface IPairingQrCodeService
{
    byte[] Generate(string ipAddress, string token, int? port = SpeechRelayNetwork.DefaultPort);
}