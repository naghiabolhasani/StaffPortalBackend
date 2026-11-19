namespace StaffPortal.Models;

public class UserInfoWithPermissions
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Username { get; set; }

    public string? Mobile { get; set; }
    public List<string> Permissions { get; set; } = new();
}