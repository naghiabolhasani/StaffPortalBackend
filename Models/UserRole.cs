using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace StaffPortal.Models;

[Table("UserRoles", Schema = "app")]
public class UserRole
{
    [Column("ID")]
    public int ID { get; set; }

    [Column("UserId")]
    public int UserId { get; set; }

    [Column("RoleId")]
    public int? RoleId { get; set; }              // ← nullable (برای مجوز مستقیم)

    [Column("CreatedAt")]
    public DateTime? CreatedAt { get; set; }

    [Column("PermissionRef")]
    public int? PermissionRef { get; set; }        // ← nullable (برای مجوز مستقیم)

    [Column("PermissionCondition")]
    public string? PermissionCondition { get; set; }

    [Column("Active")]
    public byte? Active { get; set; }              // ← nullable
}