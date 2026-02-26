using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rs_ruralia.Shared.Models;

[Table("correspondence_email")]
public class CorrespondenceEmail
{
    [Key]
    [Column("id")]
    [Display(Name = "ID")]
    public int Id { get; set; }

    [Column("email")]
    [MaxLength(50)]
    [EmailAddress]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [ForeignKey("correspondence_email_type.id")]
    [Column("correspondence_email_type_id")]
    [Required]
    [Display(Name = "Email Type")]
    public int CorrespondenceEmailTypeId { get; set; }

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