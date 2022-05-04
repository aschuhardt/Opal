using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Opal.Authentication;

public static class CertificateHelper
{
    public static X509Certificate2 GenerateNew(string subject, TimeSpan lifespan, int keySize = 2048)
    {
        // var certRequest = new CertificateRequest(new X500DistinguishedName($"CN={subject}"),
        //    ECDsa.Create(ECCurve.NamedCurves.nistP256), HashAlgorithmName.SHA256);

        var certRequest = new CertificateRequest(new X500DistinguishedName($"CN={subject}"),
            RSA.Create(keySize), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        certRequest.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.NonRepudiation | X509KeyUsageFlags.DigitalSignature, true));

        var certificate = certRequest.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.Now + lifespan);

        return certificate;
    }
}