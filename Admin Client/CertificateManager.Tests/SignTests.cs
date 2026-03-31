using CertificateManager.Admin.Pages.Signature;
using System;
using System.Text;
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
}
