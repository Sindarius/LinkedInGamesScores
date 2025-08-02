using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace game.api.Attributes
{
    public class AdminAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            
            if (!context.HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                context.Result = new UnauthorizedObjectResult(new { message = "Authorization header missing" });
                return;
            }

            var token = authHeader.ToString().Replace("Bearer ", "");
            
            if (!IsValidToken(token, configuration))
            {
                context.Result = new UnauthorizedObjectResult(new { message = "Invalid or expired token" });
                return;
            }
        }

        private bool IsValidToken(string token, IConfiguration configuration)
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
                var adminPassword = configuration["AdminSettings:Password"];

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
}