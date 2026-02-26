using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rs_ruralia.Shared.Models;

[Table("vendor_vin_code")]
public class VendorVinCode
{
    [Key]
    [Column("id")]
    [Display(Name = "ID")]
    public int Id { get; set; }

    [Column("code")]
    [MaxLength(25)]
    [Display(Name = "Code")]
    public string? Code { get; set; }

    [Column("description")]
    [MaxLength(50)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Column("order")]
    [Display(Name = "Order")]
    public int? Order { get; set; }

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