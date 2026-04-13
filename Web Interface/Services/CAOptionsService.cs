namespace CertificateManager.Services
{
    public class CAOptionsService
    {
        public string[] Options { get; }
        public string Default => Options.Length > 0 ? Options[0] : string.Empty;

        public CAOptionsService(IConfiguration configuration)
        {
            Options = configuration.GetSection("CAOptions").Get<string[]>() ?? [];
        }
    }
}
