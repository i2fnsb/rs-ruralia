using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rs_ruralia.Shared.Models;

[Table("person_profile")]
public class PersonProfile
{
    [Key]
    [Column("id")]
    [Display(Name = "ID")]
    public int Id { get; set; }

    [Column("active")]
    [Display(Name = "Active")]
    public bool? Active { get; set; }

    [Column("first_name")]
    [MaxLength(50)]
    [Display(Name = "First Name")]
    public string? FirstName { get; set; }

    [Column("last_name")]
    [MaxLength(50)]
    [Display(Name = "Last Name")]
    public string? LastName { get; set; }

    [Column("middle_name")]
    [MaxLength(50)]
    [Display(Name = "Middle Name")]
    public string? MiddleName { get; set; }

    [Column("preferred_name")]
    [MaxLength(50)]
    [Display(Name = "Preferred Name")]
    public string? PreferredName { get; set; }

    [ForeignKey("person_honorific.id")]
    [Column("person_honorific_id")]
    [Display(Name = "Honorific")]
    public int? PersonHonorificId { get; set; }

    [ForeignKey("person_suffix.id")]
    [Column("person_suffix_id")]
    [Display(Name = "Suffix")]
    public int? PersonSuffixId { get; set; }

    [Column("auth_id")]
    [Required]
    [MaxLength(450)]
    [Display(Name = "Auth ID")]
    public string AuthId { get; set; } = string.Empty;

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