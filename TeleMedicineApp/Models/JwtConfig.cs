namespace TeleMedicineApp.Models
{
    public class JwtConfig
    {
        public string Secret { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int ExpirationInMinutes { get; set; }
        
        public bool ValidateLifeTime { get; set; }
    }
}