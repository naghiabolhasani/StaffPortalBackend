using System.ComponentModel.DataAnnotations.Schema;

namespace StaffPortal.Models;

[Table("Constant")]
public class Constant
{
    public long ID { get; set; } // ← int به long تغییر یافت
    public string Title { get; set; } = string.Empty;
}