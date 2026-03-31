using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CertificateManager.Admin.Pages.Signature;

public static class SignatureHelper
{
    public static string BuildSignatureBlock(byte[] signature)
    {
        string base64Signature = Convert.ToBase64String(signature);
        var sb = new StringBuilder();
        sb.AppendLine("# SIG # Begin signature block");
        for (int i = 0; i < base64Signature.Length; i += 64)
        {
            sb.AppendLine("# " + base64Signature.Substring(i, Math.Min(64, base64Signature.Length - i)));
        }
        sb.Append("# SIG # End signature block");
        return sb.ToString();
    }

    public static string RemoveSignatureBlock(string scriptText)
    {
        var lines = scriptText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var sb = new StringBuilder();
        bool signatureLine = false;
        foreach (var line in lines)
        {
            switch (line)
            {
                case var m when m.Contains("# SIG # Begin signature block", StringComparison.OrdinalIgnoreCase):
                    signatureLine = true;
                    break;
                case var m when m.Contains("# SIG # End signature block", StringComparison.OrdinalIgnoreCase):
                    signatureLine = false;
                    break;
                default:
                    if (!signatureLine)
                    {
                        sb.AppendLine(line);
                    }
                    break;
            }
        }
        return sb.ToString().TrimEnd();
    }

    public static string SignCms(string scriptText, X509Certificate2 cert)
    {
        scriptText = scriptText.Replace("\r\n", "\n").Replace("\n", "\r\n");
        scriptText = RemoveSignatureBlock(scriptText);
        if (!scriptText.EndsWith("\r\n"))
            scriptText += "\r\n";

        byte[] scriptBytes = Encoding.UTF8.GetBytes(scriptText);

        ContentInfo contentInfo = new ContentInfo(scriptBytes);
        SignedCms signedCms = new SignedCms(contentInfo, detached: true);
        CmsSigner cmsSigner = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, cert)
        {
            IncludeOption = X509IncludeOption.EndCertOnly
        };

        signedCms.ComputeSignature(cmsSigner);
        byte[] signature = signedCms.Encode();

        string signatureBlock = BuildSignatureBlock(signature);
        return scriptText + "\r\n" + signatureBlock;
    }

    public static object SignScript(string filePath, X509Certificate2 cert, string? timestampServer = null)
    {
        using var ps = PowerShell.Create();
        ps.AddCommand("Set-AuthenticodeSignature")
          .AddParameter("FilePath", filePath)
          .AddParameter("Certificate", cert)
          .AddParameter("HashAlgorithm", "SHA1");

        if (timestampServer != null)
            ps.AddParameter("TimestampServer", timestampServer);

        var results = ps.Invoke();
        if (ps.HadErrors)
            throw new InvalidOperationException(string.Join(Environment.NewLine, ps.Streams.Error));

        if((timestampServer != null) && (results[0].Properties["TimeStamperCertificate"].Value == null))
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, "Timestampserver not reachable"));
        }

        return results[0].BaseObject;
    }

    public static object VerifyScript(string filePath)
    {
        using var ps = PowerShell.Create();
        ps.AddCommand("Get-AuthenticodeSignature")
          .AddParameter("FilePath", filePath);

        var results = ps.Invoke();
        if (ps.HadErrors)
            throw new InvalidOperationException(string.Join(Environment.NewLine, ps.Streams.Error));

        return results[0].BaseObject;
    }

    public static async System.Threading.Tasks.Task<string> SignAuthenticodeAsync(string scriptText, X509Certificate2 cert, string? timestampServer)
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"sign_{Guid.NewGuid():N}.ps1");
        await File.WriteAllTextAsync(tempFile, scriptText, new UTF8Encoding(false));

        try
        {
            SignScript(tempFile, cert, timestampServer);
            return await File.ReadAllTextAsync(tempFile);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    public static string ComputeScriptHash(string scriptText)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(scriptText)));
    }

    public static string TruncateScript(string scriptText, int maxLength = 4096)
    {
        return scriptText.Length <= maxLength ? scriptText : scriptText.Substring(0, maxLength);
    }

    public static List<CertificateInfo> GetCodeSigningCertificates(StoreLocation location)
    {
        const string codeSigOid = "1.3.6.1.5.5.7.3.3";
        var result = new List<CertificateInfo>();

        using var store = new X509Store(StoreName.My, location);
        store.Open(OpenFlags.ReadOnly);

        foreach (var cert in store.Certificates)
        {
            if (!cert.HasPrivateKey) continue;

            bool hasCodeSigning = cert.Extensions
                .OfType<X509EnhancedKeyUsageExtension>()
                .Any(eku => eku.EnhancedKeyUsages.Cast<Oid>().Any(o => o.Value == codeSigOid));
            if (!hasCodeSigning) continue;

            if (cert.NotAfter < DateTime.Now || cert.NotBefore > DateTime.Now) continue;

            result.Add(new CertificateInfo
            {
                Thumbprint = cert.Thumbprint,
                Subject = cert.GetNameInfo(X509NameType.SimpleName, false),
                Issuer = cert.GetNameInfo(X509NameType.SimpleName, true),
                NotAfter = cert.NotAfter,
                StoreLocation = location
            });
        }

        return result;
    }

}

public class CertificateInfo
{
    public string Thumbprint { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Issuer { get; set; } = "";
    public DateTime NotAfter { get; set; }
    public StoreLocation StoreLocation { get; set; }

    public override string ToString() => $"{Subject} (Issuer: {Issuer}, Expires: {NotAfter:yyyy-MM-dd})";
}
