using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rs_ruralia.Shared.Models;

[Table("service_area")]
public class ServiceArea
{
    [Key]
    [Column("id")]
    [Display(Name = "ID")]
    public int Id { get; set; }

    [Column("feature_object_id")]
    [MaxLength(75)]
    [Display(Name = "Feature Object ID", Description = "GIS feature object identifier")]
    public string? FeatureObjectId { get; set; }
    
    [Column("name")]
    [MaxLength(150)]
    [Required]
    [Display(Name = "Service Area Name", Description = "Name of the service area")]
    public string? Name { get; set; }
    
    [Column("org_key")]
    [Display(Name = "Organization Key", Description = "Organization key identifier")]
    public int? OrgKey { get; set; }
    
    [Column("current_mileage")]
    [Range(0, (double)decimal.MaxValue)]
    [Display(Name = "Current Mileage", Description = "Total road mileage")]
    public decimal? CurrentMileage { get; set; }
    
    [Column("abolish_merge_year")]
    [Range(1900, 2100)]
    [Display(Name = "Abolish/Merge Year", Description = "Year service area was abolished or merged")]
    public int? AbolishMergeYear { get; set; }
    
    [Column("maximum_mill_rate")]
    [Range(0, (double)decimal.MaxValue)]
    [Display(Name = "Maximum Mill Rate", Description = "Maximum allowed mill rate")]
    public decimal? MaximumMillRate { get; set; }
    
    [Column("current_mill_rate")]
    [Range(0, (double)decimal.MaxValue)]
    [Display(Name = "Current Mill Rate", Description = "Current assessed mill rate")]
    public decimal? CurrentMillRate { get; set; }
    
    [Column("fire_mill_rate")]
    [Range(0, (double)decimal.MaxValue)]
    [Display(Name = "Fire Mill Rate", Description = "Fire protection mill rate")]
    public decimal? FireMillRate { get; set; }
    
    [Column("mill_levy")]
    [Range(0, (double)decimal.MaxValue)]
    [Display(Name = "Mill Levy", Description = "Total mill levy amount")]
    public decimal? MillLevy { get; set; }
    
    [Column("tax_authority_code")]
    [MaxLength(25)]
    [Display(Name = "Tax Authority Code", Description = "Tax authority identifier")]
    public string? TaxAuthorityCode { get; set; }
    
    [Column("initial_tax_year")]
    [Range(1900, 2100)]
    [Display(Name = "Initial Tax Year", Description = "Year taxation began")]
    public int? InitialTaxYear { get; set; }
    
    [Column("match_feature_layer")]
    [Display(Name = "Match Feature Layer")]
    public bool? MatchFeatureLayer { get; set; }

    [ForeignKey("service_area_code.id")]
    [Column("service_area_code_id")]
    [Display(Name = "Service Area Code", Description = "Related service area code")]
    public int? ServiceAreaCodeId { get; set; }

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