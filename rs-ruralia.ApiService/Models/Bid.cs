using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rs_ruralia.Shared.Models;

[Table("bid")]
public class Bid
{
    [Key]
    [Column("id")]
    [Display(Name = "ID")]
    public int Id { get; set; }

    [Column("bid_amount")]
    [DataType(DataType.Currency)]
    [Display(Name = "Bid Amount")]
    public decimal? BidAmount { get; set; }

    [ForeignKey("rfq.id")]
    [Column("rfq_id")]
    [Required]
    [Display(Name = "RFQ")]
    public int RfqId { get; set; }

    [ForeignKey("vendor_profile.id")]
    [Column("vendor_profile_id")]
    [Required]
    [Display(Name = "Vendor Profile")]
    public int VendorProfileId { get; set; }

    [ForeignKey("specification_pay_item.id")]
    [Column("specification_pay_item_id")]
    [Required]
    [Display(Name = "Specification Pay Item")]
    public int SpecificationPayItemId { get; set; }

    [ForeignKey("bid_quantity.id")]
    [Column("bid_quantity_id")]
    [Required]
    [Display(Name = "Bid Quantity")]
    public int BidQuantityId { get; set; }

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