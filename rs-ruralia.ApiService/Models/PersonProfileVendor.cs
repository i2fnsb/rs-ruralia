using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rs_ruralia.Shared.Models;

[Table("person_profile_vendor")]
public class PersonProfileVendor
{
    [Key]
    [Column("id")]
    [Display(Name = "ID")]
    public int Id { get; set; }

    [Column("title")]
    [MaxLength(50)]
    [Display(Name = "Title")]
    public string? Title { get; set; }

    [Column("primary_contact")]
    [Display(Name = "Primary Contact")]
    public bool? PrimaryContact { get; set; }

    [Column("active")]
    [Display(Name = "Active")]
    public bool? Active { get; set; }

    [ForeignKey("person_profile.id")]
    [Column("person_profile_id")]
    [Required]
    [Display(Name = "Person Profile")]
    public int PersonProfileId { get; set; }

    [ForeignKey("vendor_profile.id")]
    [Column("vendor_profile_id")]
    [Required]
    [Display(Name = "Vendor Profile")]
    public int VendorProfileId { get; set; }

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