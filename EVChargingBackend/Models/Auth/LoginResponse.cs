namespace EVChargingBackend.Models.Auth
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public TokenData? Data { get; set; }
        public string? Error { get; set; }
    }
}
