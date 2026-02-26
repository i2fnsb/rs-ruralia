using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rs_ruralia.Shared.Models;

[Table("commissioner_status")]
public class CommissionerStatus
{
    [Key]
    [Column("id")]
    [Display(Name = "ID")]
    public int Id { get; set; }

    [Column("status")]
    [MaxLength(250)]
    [Display(Name = "Status")]
    public string? Status { get; set; }

    [Column("status_code")]
    [MaxLength(5)]
    [Display(Name = "Status Code")]
    public string? StatusCode { get; set; }

    [Column("description")]
    [MaxLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Column("active")]
    [Display(Name = "Active")]
    public bool? Active { get; set; }

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