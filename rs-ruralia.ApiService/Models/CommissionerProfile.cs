using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rs_ruralia.Shared.Models;

[Table("commissioner_profile")]
public class CommissionerProfile
{
    [Key]
    [Column("id")]
    [Display(Name = "ID")]
    public int Id { get; set; }

    [Column("oath")]
    [Display(Name = "Oath Taken", Description = "Has the commissioner taken the oath")]
    public bool? Oath { get; set; }

    [Column("invoices")]
    [Display(Name = "Invoices", Description = "Can receive invoices")]
    public bool? Invoices { get; set; }

    [Column("resides_in_service_area")]
    [Display(Name = "Resides in Service Area", Description = "Does the commissioner reside in the service area")]
    public bool? ResidesInServiceArea { get; set; }

    [Column("registered_voter")]
    [Display(Name = "Registered Voter", Description = "Is the commissioner a registered voter")]
    public bool? RegisteredVoter { get; set; }

    [Column("recommended_for_appointment")]
    [Display(Name = "Recommended for Appointment")]
    public bool? RecommendedForAppointment { get; set; }

    [Column("do_not_appoint")]
    [Display(Name = "Do Not Appoint")]
    public bool? DoNotAppoint { get; set; }

    [Column("do_not_appoint_details")]
    [MaxLength(500)]
    [Display(Name = "Do Not Appoint Details", Description = "Reason for not appointing")]
    public string? DoNotAppointDetails { get; set; }

    [Column("appointed_date")]
    [Display(Name = "Appointed Date")]
    public DateTime? AppointedDate { get; set; }

    [Column("assembly_meeting_date")]
    [Display(Name = "Assembly Meeting Date")]
    public DateTime? AssemblyMeetingDate { get; set; }

    [Column("effective_date")]
    [Display(Name = "Effective Date")]
    public DateTime? EffectiveDate { get; set; }

    [Column("end_date")]
    [Display(Name = "End Date")]
    public DateTime? EndDate { get; set; }

    [ForeignKey("commissioner_status.id")]
    [Column("commissioner_status_id")]
    [Required]
    [Display(Name = "Commissioner Status")]
    public int CommissionerStatusId { get; set; }

    [ForeignKey("commission_seat.id")]
    [Column("commission_seat_id")]
    [Required]
    [Display(Name = "Commission Seat")]
    public int CommissionSeatId { get; set; }

    [ForeignKey("person_profile.id")]
    [Column("person_profile_id")]
    [Required]
    [Display(Name = "Person Profile")]
    public int PersonProfileId { get; set; }

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