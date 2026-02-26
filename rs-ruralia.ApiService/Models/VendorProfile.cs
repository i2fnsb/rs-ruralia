using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rs_ruralia.Shared.Models;

[Table("vendor_profile")]
public class VendorProfile
{
    [Key]
    [Column("id")]
    [Display(Name = "ID")]
    public int Id { get; set; }

    [Column("active")]
    [Display(Name = "Active")]
    public bool? Active { get; set; }

    [Column("name")]
    [MaxLength(250)]
    [Display(Name = "Name")]
    public string? Name { get; set; }

    [Column("doing_business_as")]
    [Display(Name = "Doing Business As (DBA)")]
    public bool? DoingBusinessAs { get; set; }

    [Column("vendor_id")]
    [MaxLength(100)]
    [Display(Name = "Vendor ID")]
    public string? VendorId { get; set; }

    [Column("license_on_file")]
    [Display(Name = "License on File")]
    public bool? LicenseOnFile { get; set; }

    [Column("license_expiration_date")]
    [Display(Name = "License Expiration Date")]
    public DateTime? LicenseExpirationDate { get; set; }

    [Column("contractor_license")]
    [Display(Name = "Contractor License")]
    public bool? ContractorLicense { get; set; }

    [Column("contractor_license_expiration_date")]
    [Display(Name = "Contractor License Expiration Date")]
    public DateTime? ContractorLicenseExpirationDate { get; set; }

    [ForeignKey("vendor_type.id")]
    [Column("vendor_type_id")]
    [Display(Name = "Vendor Type")]
    public int? VendorTypeId { get; set; }

    [ForeignKey("vendor_vin_code.id")]
    [Column("vendor_vin_code_id")]
    [Display(Name = "VIN Code")]
    public int? VendorVinCodeId { get; set; }

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