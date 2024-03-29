﻿using System;
using System.Security.Cryptography.X509Certificates;

#if NETSTANDARD2_0
#else
using System.Text;
using System.Security.Cryptography;
#endif

namespace Opal.Authentication
{
    public static class CertificateHelper
    {
        /// <summary>
        ///     Generates and returns a new self-signed request with the provided attributes
        /// </summary>
        /// <param name="lifespan">How long the certificate should be valid for</param>
        /// <param name="name">A name to associate with the certificate (CN)</param>
        /// <param name="emailAddress">An optional email address to associate with the certificate (E)</param>
        /// <param name="keySize">The size of the certificate's RSA signing key in bits</param>
        /// <returns>A new self-signed certificate</returns>
        /// <exception cref="ArgumentNullException">Thrown if <see cref="name" /> is null or whitespace</exception>
        public static X509Certificate2 GenerateNew(TimeSpan lifespan, string name, string emailAddress = null,
            int keySize = 2048)
        {
#if NETSTANDARD2_0
            throw new NotImplementedException(
                "Certificate generation is not implemented for the .NET Standard 2.0 version of Opal.");
#else
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name), "A name is required for generating certificates");

        var subject = new StringBuilder($"CN={name}");
        if (!string.IsNullOrWhiteSpace(emailAddress))
            subject.Append($", E={emailAddress}");

        var certRequest = new CertificateRequest(new X500DistinguishedName(subject.ToString()),
            RSA.Create(keySize), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        certRequest.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.NonRepudiation | X509KeyUsageFlags.DigitalSignature, true));

        var certificate = certRequest.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow + lifespan);

        return certificate;
#endif
        }

        public static X509Certificate2 Renew(TimeSpan lifespan, X509Certificate2 certificate)
        {
#if NETSTANDARD2_0
            throw new NotImplementedException(
                "Certificate renewal is not implemented for the .NET Standard 2.0 version of Opal.");
#else
        var key = certificate.GetRSAPrivateKey()
                  ?? throw new ArgumentNullException(nameof(certificate),
                      "The certificate is missing its private RSA key");

        var certRequest = new CertificateRequest(certificate.SubjectName, key,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return certRequest.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow + lifespan);
#endif
        }
    }
}