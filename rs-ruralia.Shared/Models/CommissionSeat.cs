using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rs_ruralia.Shared.Models;

[Table("commission_seat")]
public class CommissionSeat
{
    [Key]
    [Column("id")]
    [Display(Name = "ID")]
    public int Id { get; set; }

    [Column("term_start_date")]
    [Display(Name = "Term Start Date", Description = "Start date of the commission seat term (must be July 1st)")]
    public DateTime? TermStartDate { get; set; }

    [Column("term_end_date")]
    [Display(Name = "Term End Date", Description = "End date of the commission seat term (must be June 30th)")]
    public DateTime? TermEndDate { get; set; }

    [Column("creation_date")]
    [Display(Name = "Creation Date", Description = "Date when the seat was created")]
    public DateTime? CreationDate { get; set; }

    [Column("termination_date")]
    [Display(Name = "Termination Date", Description = "Date when the seat was terminated")]
    public DateTime? TerminationDate { get; set; }

    [ForeignKey("commission_seat_type.id")]
    [Column("commission_seat_type_id")]
    [Required]
    [Display(Name = "Seat Type")]
    public int CommissionSeatTypeId { get; set; }

    [ForeignKey("commission_seat_class.id")]
    [Column("commission_seat_class_id")]
    [Required]
    [Display(Name = "Seat Class")]
    public int CommissionSeatClassId { get; set; }

    [ForeignKey("commission_seat_status.id")]
    [Column("commission_seat_status_id")]
    [Required]
    [Display(Name = "Seat Status")]
    public int CommissionSeatStatusId { get; set; }

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