using Microsoft.AspNetCore.Mvc;

namespace game.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AdminController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("authenticate")]
        public ActionResult<AdminAuthResponse> Authenticate([FromBody] AdminAuthRequest request)
        {
            var adminPassword = _configuration["AdminSettings:Password"];
            
            if (string.IsNullOrEmpty(adminPassword))
            {
                return StatusCode(500, "Admin password not configured");
            }

            if (request.Password == adminPassword)
            {
                var token = GenerateSimpleToken();
                
                return Ok(new AdminAuthResponse 
                { 
                    Success = true, 
                    Token = token,
                    Message = "Authentication successful"
                });
            }

            return Unauthorized(new AdminAuthResponse 
            { 
                Success = false, 
                Message = "Invalid password" 
            });
        }

        [HttpPost("validate")]
        public ActionResult<AdminAuthResponse> ValidateToken([FromBody] AdminTokenRequest request)
        {
            if (IsValidToken(request.Token))
            {
                return Ok(new AdminAuthResponse 
                { 
                    Success = true, 
                    Message = "Token is valid" 
                });
            }

            return Unauthorized(new AdminAuthResponse 
            { 
                Success = false, 
                Message = "Invalid or expired token" 
            });
        }

        private string GenerateSimpleToken()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var secret = _configuration["AdminSettings:Password"];
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"admin:{timestamp}:{secret}"));
        }

        private bool IsValidToken(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return false;

                var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
                var parts = decoded.Split(':');
                
                if (parts.Length != 3 || parts[0] != "admin")
                    return false;

                var timestamp = long.Parse(parts[1]);
                var secret = parts[2];
                var adminPassword = _configuration["AdminSettings:Password"];

                if (secret != adminPassword)
                    return false;

                var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var tokenAge = currentTimestamp - timestamp;
                
                return tokenAge < 86400; // Token valid for 24 hours
            }
            catch
            {
                return false;
            }
        }
    }

    public class AdminAuthRequest
    {
        public string Password { get; set; } = string.Empty;
    }

    public class AdminTokenRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    public class AdminAuthResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}