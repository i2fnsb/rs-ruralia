using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rs_ruralia.Shared.Models;

[Table("note")]
public class Note
{
    [Key]
    [Column("id")]
    [Display(Name = "ID")]
    public int Id { get; set; }

    [Column("note")]
    [Display(Name = "Note", Description = "Note text content")]
    public string? NoteText { get; set; }

    [Column("road_id")]
    [Display(Name = "Road ID")]
    public int? RoadId { get; set; }

    [Column("service_area_id")]
    [Display(Name = "Service Area ID")]
    public int? ServiceAreaId { get; set; }

    [Column("commissioner_profile_id")]
    [Display(Name = "Commissioner Profile ID")]
    public int? CommissionerProfileId { get; set; }

    [Column("commission_seat_id")]
    [Display(Name = "Commission Seat ID")]
    public int? CommissionSeatId { get; set; }

    [Column("correspondence_profile_id")]
    [Display(Name = "Correspondence Profile ID")]
    public int? CorrespondenceProfileId { get; set; }

    [Column("person_profile_id")]
    [Display(Name = "Person Profile ID")]
    public int? PersonProfileId { get; set; }

    [Column("vendor_profile_id")]
    [Display(Name = "Vendor Profile ID")]
    public int? VendorProfileId { get; set; }

    [Column("rfq_id")]
    [Display(Name = "RFQ ID")]
    public int? RfqId { get; set; }

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
