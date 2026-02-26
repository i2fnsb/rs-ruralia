using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rs_ruralia.Shared.Models;

[Table("road_road_subdivision")]
public class RoadRoadSubdivision
{
    [Key]
    [Column("id")]
    [Display(Name = "ID")]
    public int Id { get; set; }

    [ForeignKey("road_subdivision.id")]
    [Column("road_subdivision_id")]
    [Required]
    [Display(Name = "Road Subdivision")]
    public int RoadSubdivisionId { get; set; }

    [ForeignKey("road.id")]
    [Column("road_id")]
    [Required]
    [Display(Name = "Road")]
    public int RoadId { get; set; }

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