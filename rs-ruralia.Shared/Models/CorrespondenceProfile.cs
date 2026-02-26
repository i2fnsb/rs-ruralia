using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rs_ruralia.Shared.Models;

[Table("correspondence_profile")]
public class CorrespondenceProfile
{
    [Key]
    [Column("id")]
    [Display(Name = "ID")]
    public int Id { get; set; }

    [Column("phone")]
    [Display(Name = "Phone Enabled")]
    public bool? Phone { get; set; }

    [Column("email")]
    [Display(Name = "Email Enabled")]
    public bool? Email { get; set; }

    [Column("fax")]
    [Display(Name = "Fax Enabled")]
    public bool? Fax { get; set; }

    [Column("mailing_address")]
    [Display(Name = "Mailing Address Enabled")]
    public bool? MailingAddress { get; set; }

    [Column("public_profile")]
    [Display(Name = "Public Profile")]
    public bool? PublicProfile { get; set; }

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

    [ForeignKey("correspondence_phone.id")]
    [Column("correspondence_phone_id")]
    [Display(Name = "Correspondence Phone")]
    public int? CorrespondencePhoneId { get; set; }

    [ForeignKey("correspondence_email.id")]
    [Column("correspondence_email_id")]
    [Display(Name = "Correspondence Email")]
    public int? CorrespondenceEmailId { get; set; }

    [ForeignKey("correspondence_address.id")]
    [Column("correspondence_address_id")]
    [Display(Name = "Correspondence Address")]
    public int? CorrespondenceAddressId { get; set; }

    [ForeignKey("correspondence_phone.id")]
    [Column("correspondence_fax_id")]
    [Display(Name = "Correspondence Fax")]
    public int? CorrespondenceFaxId { get; set; }

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