using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rs_ruralia.Shared.Models;

[Table("vendor_type")]
public class VendorType
{
    [Key]
    [Column("id")]
    [Display(Name = "ID")]
    public int Id { get; set; }

    [Column("type")]
    [MaxLength(100)]
    [Display(Name = "Type")]
    public string? Type { get; set; }

    [Column("ModifiedBy")]
    [MaxLength(100)]
    [Display(Name = "Modified By")]
    [EmailAddress]
    public string? ModifiedBy { get; set; }

    [Column("ValidFrom")]
    [Display(Name = "Valid From")]
    public DateTime ValidFrom { get; set; }

    [Column("ValidTo")]
    [Display(Name = "Valid To")]
    public DateTime ValidTo { get; set; }
}