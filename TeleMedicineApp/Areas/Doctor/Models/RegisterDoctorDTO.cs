public class RegisterDoctorDTO
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string Gender { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string LicenseNumber { get; set; }
    public string MedicalCollege { get; set; }
    public string Specialization { get; set; }
    public int YearsOfExperience { get; set; }
    public string ClinicName { get; set; }
    public string ClinicAddress { get; set; }
    public decimal ConsultationFee { get; set; }
}