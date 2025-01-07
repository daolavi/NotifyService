using EllipticCurve;

namespace NotifyService.Api.Services;

public interface ISendGridSignatureValidationService
{
    bool IsValidSignature(string timestamp, string payload, string providedSignature, string verificationKey);
}

public class SendGridSignatureValidationService : ISendGridSignatureValidationService
{
    public bool IsValidSignature(string timestamp, string payload, string providedSignature, string verificationKey)
    {
        var data = $"{timestamp}{payload}";

        var publicKey = PublicKey.fromPem(verificationKey);
        var decodedSignature = Signature.fromBase64(providedSignature);
    
        return Ecdsa.verify(data, decodedSignature, publicKey);
    }
}