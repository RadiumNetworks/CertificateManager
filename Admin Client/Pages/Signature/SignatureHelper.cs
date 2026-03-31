using System;
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
}
