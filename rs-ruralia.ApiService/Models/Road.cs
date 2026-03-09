using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rs_ruralia.Shared.Models;

[Table("road")]
public class Road
{
  [Key]
  [Column("id")]
  [Display(Name = "ID")]
  public int Id { get; set; }

  [Column("phase")]
  [Display(Name = "Phase")]
  public int? Phase { get; set; }

  [Column("distance_feet")]
  [Display(Name = "Distance (Feet)")]
  public double? DistanceFeet { get; set; }

  [Column("start_description")]
  [MaxLength(250)]
  [Display(Name = "Start Description")]
  public string? StartDescription { get; set; }

  [Column("end_description")]
  [MaxLength(250)]
  [Display(Name = "End Description")]
  public string? EndDescription { get; set; }

  [Column("start_coordinates")]
  [Display(Name = "Start Coordinates")]
  public string? StartCoordinates { get; set; } // geography type stored as string

  [Column("end_coordinates")]
  [Display(Name = "End Coordinates")]
  public string? EndCoordinates { get; set; } // geography type stored as string

  [Column("road_location_coordinates")]
  [Display(Name = "Road Location")]
  public string? RoadLocation { get; set; } // geography type stored as string

    [Column("approved_for_maintenance")]
  [Display(Name = "Approved for Maintenance")]
  public bool? ApprovedForMaintenance { get; set; }

  [Column("mpo")]
  [Display(Name = "MPO")]
  public bool? Mpo { get; set; }

  [Column("legacy_subdivision_name")]
  [MaxLength(100)]
  [Display(Name = "Legacy Subdivision Name")]
  public string? LegacySubdivisionName { get; set; }

  [ForeignKey("road_name.id")]
  [Column("road_name_id")]
  [Display(Name = "Road Name")]
  public int? RoadNameId { get; set; }

  [ForeignKey("road_surface_type.id")]
  [Column("road_surface_type_id")]
  [Display(Name = "Surface Type")]
  public int? RoadSurfaceTypeId { get; set; }

  [ForeignKey("road_responder_code.id")]
  [Column("responder_code_id")]
  [Display(Name = "Responder Code")]
  public int? ResponderCodeId { get; set; }

  [ForeignKey("service_area.id")]
  [Column("service_area_id")]
  [Required]
  [Display(Name = "Service Area")]
  public int ServiceAreaId { get; set; }

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