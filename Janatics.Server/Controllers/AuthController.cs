namespace Janatics.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using System.IdentityModel.Tokens.Jwt;
    using Microsoft.IdentityModel.Tokens;
    using System.Text;
    using System.Security.Claims;
    using System.Linq;

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;

        public AuthController(IConfiguration config)
        {
            _config = config;
        }

        public class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest req)
        {
            var users = new[] {
                new { Username = "admin", Password = "admin", Role = "Admin", Id = "1", Name = "Administrator", Email = "admin@example.com" },
                new { Username = "user", Password = "user", Role = "User", Id = "2", Name = "Regular User", Email = "user@example.com" }
            };

            var user = users.FirstOrDefault(u => u.Username == req.Username && u.Password == req.Password);
            if (user == null)
            {
                return Unauthorized(new { error = "Invalid credentials" });
            }

            var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"] ?? "super_secret_dev_key_please_change");
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = System.DateTime.UtcNow.AddHours(8),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new { token = tokenString, user = new { id = user.Id, name = user.Name, email = user.Email, role = user.Role } });
        }
    }
}
