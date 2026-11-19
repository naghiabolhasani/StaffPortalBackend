using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace StaffPortal.Models;

[Table("Roles", Schema = "app")]
public class Role
{
    [Column("Id")]
    public int Id { get; set; }

    [Column("Name")]
    public string Name { get; set; } = string.Empty;

    [Column("DisplayName")]
    public string? DisplayName { get; set; }

    [Column("Dsr")]
    public string? Dsr { get; set; }

    [Column("Active")]
    public byte? Active { get; set; }              // ← اضافه شد

    [Column("UpsertDate")]
    public DateTime UpsertDate { get; set; }
}