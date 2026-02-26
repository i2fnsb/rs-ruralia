using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rs_ruralia.Shared.Models;

[Table("service_area_code")]
public class ServiceAreaCode
{
    [Key]
    [Column("id")]
    [Display(Name = "ID")]
    public int Id { get; set; }

    [Column("code")]
    [MaxLength(1)]
    [Required]
    [Display(Name = "Code", Description = "Code of the service area")]
    public string? Code { get; set; }

    [Column("description")]
    [MaxLength(250)]
    [Required]
    [Display(Name = "Code Description", Description = "Description of the service area code")]
    public string? Description { get; set; }

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