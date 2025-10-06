namespace EVChargingBackend.Models.Auth
{
    public class TokenData
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Nic { get; set; } = string.Empty;
        public string ExpiresAt { get; set; } = string.Empty;
    }
}
