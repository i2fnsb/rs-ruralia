using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rs_ruralia.Shared.Models;

[Table("rfq")]
public class Rfq
{
    [Key]
    [Column("id")]
    [Display(Name = "ID")]
    public int Id { get; set; }

    [Column("rfq_number")]
    [MaxLength(50)]
    [Display(Name = "RFQ Number")]
    public string? RfqNumber { get; set; }

    [Column("issue_date")]
    [Display(Name = "Issue Date")]
    public DateTime? IssueDate { get; set; }

    [Column("opening_date")]
    [Display(Name = "Opening Date")]
    public DateTime? OpeningDate { get; set; }

    [Column("termination_date")]
    [Display(Name = "Termination Date")]
    public DateTime? TerminationDate { get; set; }

    [Column("term_fy")]
    [Display(Name = "Term Fiscal Year")]
    public int? TermFy { get; set; }

    [Column("renewal_options")]
    [Display(Name = "Renewal Options")]
    public int? RenewalOptions { get; set; }

    [Column("custom_project_scope")]
    [MaxLength(250)]
    [Display(Name = "Custom Project Scope")]
    public string? CustomProjectScope { get; set; }

    [Column("special_instructions")]
    [MaxLength(250)]
    [Display(Name = "Special Instructions")]
    public string? SpecialInstructions { get; set; }

    [Column("completed")]
    [Display(Name = "Completed")]
    public bool? Completed { get; set; }

    [Column("bid_revised_date")]
    [Display(Name = "Bid Revised Date")]
    public DateTime? BidRevisedDate { get; set; }

    [Column("rebid_year")]
    [Display(Name = "Rebid Year")]
    public int? RebidYear { get; set; }

    [ForeignKey("rfq_project_scope.id")]
    [Column("rfq_project_scope_id")]
    [Required]
    [Display(Name = "Project Scope")]
    public int RfqProjectScopeId { get; set; }

    [ForeignKey("rfq_ifb_type.id")]
    [Column("rfq_ifb_type_id")]
    [Required]
    [Display(Name = "IFB Type")]
    public int RfqIfbTypeId { get; set; }

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