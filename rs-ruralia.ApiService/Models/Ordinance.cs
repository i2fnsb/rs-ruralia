using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rs_ruralia.Shared.Models;

[Table("ordinance")]
public class Ordinance
{
    [Key]
    [Column("id")]
    [Display(Name = "ID")]
    public int Id { get; set; }

    [Column("ordinance")]
    [MaxLength(10)]
    [Required]
    [Display(Name = "Ordinance", Description = "Ordinance name")]
    public string? OrdinanceName { get; set; }

    [Column("ordinance_year")]
    [Range(1900, 2100)]
    [Required]
    [Display(Name = "Ordinance Year", Description = "Year ordinance was passed")]
    public int? OrdinanceYear { get; set; }

    [Column("ordinance_passed")]
    [Display(Name = "Ordinance Passed", Description = "Marks if ordinance has passed")]
    public bool? OrdinancePassed { get; set; }

    [ForeignKey("ordinacne_type.id")]
    [Column("ordinance_type_id")]
    [Display(Name = "Ordinance Type", Description = "Type of ordinance")]
    public int? OrdinanceTypeId { get; set; }

    [ForeignKey("service_area.id")]
    [Column("service_area_id")]
    [Display(Name = "Service Area", Description = "Service Area")]
    public int? ServiceAreaId { get; set; }

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