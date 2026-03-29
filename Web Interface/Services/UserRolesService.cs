namespace CertificateManager.Services;

public class UserRolesService
{
    public IEnumerable<string> Roles { get; private set; } = Enumerable.Empty<string>();
    public string Username { get; private set; } = string.Empty;
    public bool IsInitialized { get; private set; }

    public void Initialize(IEnumerable<string> roles, string username)
    {
        Roles = roles;
        Username = username;
        IsInitialized = true;
    }
}
