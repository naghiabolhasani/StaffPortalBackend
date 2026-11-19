using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace StaffPortal.Models;

[Table("RolePermissions", Schema = "app")]
public class RolePermission
{
    [Column("Id")]
    public int Id { get; set; }

    [Column("RoleId")]
    public int RoleId { get; set; }

    [Column("PermissionId")]
    public int PermissionId { get; set; }

    [Column("CreatedAt")]
    public DateTime? CreatedAt { get; set; }

    [Column("Active")]
    public byte? Active { get; set; }
}


public class UserPermissionDto
{
    public string Resource { get; set; } = string.Empty;
    public string? Action { get; set; }             // null = گره منو
    public string? Condition { get; set; }          // شرط اختصاصی
    public string? ResourceFa { get; set; }
    public string? ActionFa { get; set; }
    public string? MenuTitle { get; set; }
    public long? ParentRef { get; set; }
    public int? SortIndex { get; set; }
    public bool IsMenuNode => string.IsNullOrEmpty(Action);  // ← گره منو
}