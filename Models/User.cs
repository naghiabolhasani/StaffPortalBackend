using System.ComponentModel.DataAnnotations.Schema;

namespace StaffPortal.Models;

[Table("Users", Schema = "app")]
public class User
{
    [Column("ID")]
    public int Id { get; set; }

    [Column("FullName")]
    public string FullName { get; set; } = string.Empty;

    [Column("Mobile")]
    public string? Mobile { get; set; }

    [Column("Username")]
    public string? Username { get; set; }

    [Column("PasswordHash")]
    public string? PasswordHash { get; set; }

    [Column("Active")]
    public byte Active { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [Column("DarikUserRef")]
    public long? DarikUserRef { get; set; }
}

public class SetPasswordRequest
{
    public int Id { get; set; }
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserByUsernameRequest
{
    public string Username { get; set; } = string.Empty;
}

public class UserInfoSimple
{
    public int Id { get; set; }
    public string Mobile { get; set; } = string.Empty;
}