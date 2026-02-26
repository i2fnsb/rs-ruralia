using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rs_ruralia.Shared.Models;

[Table("rfq_vendor_distribution")]
public class RfqVendorDistribution
{
    [Key]
    [Column("id")]
    [Display(Name = "ID")]
    public int Id { get; set; }

    [Column("response_received")]
    [Display(Name = "Response Received")]
    public bool? ResponseReceived { get; set; }

    [Column("declared_non_responsive")]
    [Display(Name = "Declared Non-Responsive")]
    public bool? DeclaredNonResponsive { get; set; }

    [Column("awarded")]
    [Display(Name = "Awarded")]
    public bool? Awarded { get; set; }

    [ForeignKey("rfq.id")]
    [Column("rfq_id")]
    [Required]
    [Display(Name = "RFQ")]
    public int RfqId { get; set; }

    [ForeignKey("vendor_profile.id")]
    [Column("vendor_profile_id")]
    [Required]
    [Display(Name = "Vendor Profile")]
    public int VendorProfileId { get; set; }

    [ForeignKey("person_profile_vendor.id")]
    [Column("person_profile_vendor_id")]
    [Display(Name = "Contact Person")]
    public int? PersonProfileVendorId { get; set; }

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