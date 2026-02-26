using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rs_ruralia.Shared.Models;

[Table("specification_pay_item")]
public class SpecificationPayItem
{
    [Key]
    [Column("id")]
    [Display(Name = "ID")]
    public int Id { get; set; }

    [Column("pay_item_number")]
    [MaxLength(15)]
    [Display(Name = "Pay Item Number")]
    public string? PayItemNumber { get; set; }

    [Column("pay_item")]
    [MaxLength(250)]
    [Display(Name = "Pay Item")]
    public string? PayItem { get; set; }

    [Column("pay_item_2")]
    [MaxLength(250)]
    [Display(Name = "Pay Item 2")]
    public string? PayItem2 { get; set; }

    [Column("original_published")]
    [Display(Name = "Original Published")]
    public bool? OriginalPublished { get; set; }

    [Column("special_conditions")]
    [Display(Name = "Special Conditions")]
    public bool? SpecialConditions { get; set; }

    [Column("conditions_description")]
    [MaxLength(250)]
    [Display(Name = "Conditions Description")]
    public string? ConditionsDescription { get; set; }

    [ForeignKey("specification_pay_item_type.id")]
    [Column("specification_pay_item_type_id")]
    [Required]
    [Display(Name = "Pay Item Type")]
    public int SpecificationPayItemTypeId { get; set; }

    [ForeignKey("specification_pay_unit_type.id")]
    [Column("specification_pay_unit_type_id")]
    [Required]
    [Display(Name = "Pay Unit Type")]
    public int SpecificationPayUnitTypeId { get; set; }

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