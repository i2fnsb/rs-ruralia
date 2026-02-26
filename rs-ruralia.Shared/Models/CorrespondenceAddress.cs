using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rs_ruralia.Shared.Models;

[Table("correspondence_address")]
public class CorrespondenceAddress
{
    [Key]
    [Column("id")]
    [Display(Name = "ID")]
    public int Id { get; set; }

    [Column("address")]
    [MaxLength(100)]
    [Display(Name = "Address")]
    public string? Address { get; set; }

    [Column("city")]
    [MaxLength(25)]
    [Display(Name = "City")]
    public string? City { get; set; }

    [Column("state")]
    [MaxLength(2)]
    [Display(Name = "State")]
    public string? State { get; set; }

    [Column("zip")]
    [MaxLength(20)]
    [Display(Name = "ZIP Code")]
    public string? Zip { get; set; }

    [Column("zip_ext")]
    [MaxLength(10)]
    [Display(Name = "ZIP Extension")]
    public string? ZipExt { get; set; }

    [Column("country")]
    [MaxLength(2)]
    [Display(Name = "Country")]
    public string? Country { get; set; }

    [ForeignKey("correspondence_address_type.id")]
    [Column("correspondence_address_type_id")]
    [Required]
    [Display(Name = "Address Type")]
    public int CorrespondenceAddressTypeId { get; set; }

    [ForeignKey("person_profile.id")]
    [Column("person_profile_id")]
    [Display(Name = "Person Profile")]
    public int? PersonProfileId { get; set; }

    [ForeignKey("commissioner_profile.id")]
    [Column("commissioner_profile_id")]
    [Display(Name = "Commissioner Profile")]
    public int? CommissionerProfileId { get; set; }

    [ForeignKey("vendor_profile.id")]
    [Column("vendor_profile_id")]
    [Display(Name = "Vendor Profile")]
    public int? VendorProfileId { get; set; }

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