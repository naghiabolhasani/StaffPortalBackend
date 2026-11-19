using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace StaffPortal.Models;

[Table("Permissions", Schema = "app")]
public class Permission
{
    [Column("ID")]
    public int ID { get; set; }

    [Column("Name")]
    public string Name { get; set; } = string.Empty;

    [Column("DisplayName")]
    public string? DisplayName { get; set; }

    [Column("Resource")]
    public string? Resource { get; set; }

    [Column("Action")]
    public string? Action { get; set; }

    [Column("CreatedAt")]
    public DateTime? CreatedAt { get; set; }

    [Column("ResourceFa")]
    public string? ResourceFa { get; set; }

    [Column("ActionFa")]
    public string? ActionFa { get; set; }

    [Column("ParentRef")]
    public long? ParentRef { get; set; }

    [Column("SortIndex")]
    public int? SortIndex { get; set; }

    [Column("MenuTitle")]
    public string? MenuTitle { get; set; }
}