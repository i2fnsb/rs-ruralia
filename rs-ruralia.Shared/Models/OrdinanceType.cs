using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rs_ruralia.Shared.Models;

[Table("ordinance_type")]
public class OrdinanceType
{
    [Key]
    [Column("id")]
    [Display(Name = "ID")]
    public int Id { get; set; }

    [Column("type")]
    [MaxLength(1)]
    [Required]
    [Display(Name = "Type", Description = "Type of the ordinance")]
    public string? Type { get; set; }

    [Column("description")]
    [MaxLength(100)]
    [Required]
    [Display(Name = "Ordinance Type Description", Description = "Description of the ordinance type")]
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