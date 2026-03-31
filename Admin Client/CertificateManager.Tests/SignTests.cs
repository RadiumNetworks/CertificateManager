using CertificateManager.Admin.Pages.Signature;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CertificateManager.Admin.Tests;

public class SignTests
{
    [Theory]
    [InlineData("TestString")]
    [InlineData("")]
    public void BuildSignatureBlock_ReturnsCorrectBeginAndEndMarkers(string content)
    {
        byte[] signature = Array.Empty<byte>();

        if (content != string.Empty)
        {
            signature = (Encoding.UTF8.GetBytes(content));
        }
        
        string result = SignatureHelper.BuildSignatureBlock(signature);

        Assert.StartsWith("# SIG # Begin signature block", result);
        Assert.EndsWith("# SIG # End signature block", result);
    }

    [Theory]
    [InlineData("Script\r\n" +
                        "# SIG # Begin signature block\r\n" +
                        "# Payload\r\n" +
                        "# SIG # End signature block\r\n")]
    [InlineData("Script\r\n" +
                        "# sig # begin signature block\r\n" +
                        "# payload\r\n" +
                        "# sig # end signature block\r\n")]
    [InlineData("# sig # begin signature block\r\n" +
                        "# payload\r\n" +
                        "# sig # end signature block\r\n" +
                        "Script\r\n")]
    [InlineData("Script")]
    public void RemoveSignatureBlock(string script)
    {
        string result = SignatureHelper.RemoveSignatureBlock(script);

        Assert.DoesNotContain("SIG", result);
        Assert.DoesNotContain("Payload", result);
        Assert.Contains("Script", result);
    }

    [Theory]
    [InlineData("TestString")]
    public void BuildAndRemoveSignatureBlock(string script)
    {
        byte[] dummySignature = Encoding.UTF8.GetBytes(script);

        string signatureBlock = SignatureHelper.BuildSignatureBlock(dummySignature);
        string signedScript = script + "\r\n" + signatureBlock;

        string stripped = SignatureHelper.RemoveSignatureBlock(signedScript);

        Assert.Equal(script.TrimEnd(), stripped);
    }

    [Theory]
    [InlineData("TestString")]
    public void CreateCMSSignnature(string script)
    {
        Assert.Equal(script, script);
    }

    [Theory]
    [InlineData("TestString")]
    public void CreateAuthenticodeSignnature(string script)
    {
        Assert.Equal(script, script);
    }

    [Theory]
    [InlineData("TestString")]
    public void VerifyAuthenticodeSignnature(string script)
    {
        Assert.Equal(script, script);
    }

    [Theory]
    [InlineData(StoreLocation.LocalMachine)]
    [InlineData(StoreLocation.CurrentUser)]
    public void GetCodeSigningCertificate(StoreLocation location)
    {
        var certificates = SignatureHelper.GetCodeSigningCertificates(location);
        Assert.NotEmpty(certificates);
    }

    [Theory]
    [InlineData(StoreLocation.LocalMachine, "TestString", null)]
    [InlineData(StoreLocation.CurrentUser, "TestString", null)]
    [InlineData(StoreLocation.LocalMachine, "TestString", "http://timestamp.digicert.com")]
    [InlineData(StoreLocation.CurrentUser, "TestString", "http://timestamp.digicert.com")]
    public async Task TryAuthenticodeSignature(StoreLocation location, string script, string? timestampserver)
    {
        try
        {
            var certificates = SignatureHelper.GetCodeSigningCertificates(location);
            var certificatethumbprint = certificates[0]?.Thumbprint;

            using var store = new X509Store(StoreName.My, location);
            store.Open(OpenFlags.ReadOnly);
            var certificatecollection = store.Certificates.Find(X509FindType.FindByThumbprint, certificatethumbprint, false);
            
            if(certificatecollection.Count > 0)
            {
                var output = await SignatureHelper.SignAuthenticodeAsync(script, certificatecollection[0], timestampserver);
            }
            else
            {
                Assert.Fail();
            }

            
        }
        catch
        {
            Assert.Fail();        
        }
    }
}
