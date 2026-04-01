using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace SoftZorg.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.UserName!),
                    new Claim(ClaimTypes.Email, user.Email!),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var token = GetToken(authClaims);

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo,
                    roles = userRoles // Stuurt de rollen (bijv. ["Verpleegkundige"]) terug naar de frontend
                });
            }

            return Unauthorized(new { message = "E-mailadres of wachtwoord is onjuist." });
        }

		[HttpPost("register")]
		public async Task<IActionResult> Register([FromBody] RegisterModel model)
		{
			try
			{
				// 1. Check of gebruiker al bestaat
				var userExists = await _userManager.FindByEmailAsync(model.Email);
				if (userExists != null)
					return Conflict(new { message = "Er bestaat al een account met dit e-mailadres." });

				// 2. Gebruiker aanmaken
				IdentityUser user = new()
				{
					Email = model.Email,
					SecurityStamp = Guid.NewGuid().ToString(),
					UserName = model.Email
				};

				var result = await _userManager.CreateAsync(user, model.Password);

				if (!result.Succeeded)
				{
					var errors = string.Join(" ", result.Errors.Select(e => e.Description));
					return BadRequest(new { message = $"Account aanmaken mislukt: {errors}" });
				}

				// 3. ROL-CHECK: Cruciaal voor MonsterASP / Productie
				// Controleer of de rol bestaat, anders crash je op de database-relatie
				const string defaultRole = "Verpleegkundige";
				if (!await _roleManager.RoleExistsAsync(defaultRole))
				{
					await _roleManager.CreateAsync(new IdentityRole(defaultRole));
				}

				// 4. Rol toewijzen
				var roleResult = await _userManager.AddToRoleAsync(user, defaultRole);
				if (!roleResult.Succeeded)
				{
					return StatusCode(500, new { message = "Gebruiker is aangemaakt, maar rol toewijzen is mislukt." });
				}

				return Ok(new { message = "Account succesvol aangemaakt!" });
			}
			catch (Exception ex)
			{
				// Dit vangt de ERR_CONNECTION_RESET op en vertelt je WAAROM het gebeurt
				// Kijk in je browser console naar de 'response' body
				return StatusCode(500, new
				{
					message = "Er is een serverfout opgetreden.",
					error = ex.Message,
					inner = ex.InnerException?.Message
				});
			}
		}

		// --- ADMIN ENDPOINTS ---
		[Authorize(Roles = "Admin")]
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = _userManager.Users.ToList();
            var userList = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new
                {
                    id = user.Id,
                    email = user.Email,
                    role = roles.FirstOrDefault() ?? "Geen Rol"
                });
            }

            return Ok(userList);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("users/{userId}/role")]
        public async Task<IActionResult> UpdateUserRole(string userId, [FromBody] UpdateRoleModel model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "Gebruiker niet gevonden." });

            // Ensure the role exists
            var roleExists = await _roleManager.RoleExistsAsync(model.Role);
            if (!roleExists)
            {
                // Create the role if it somehow doesn't exist in the DB yet
                await _roleManager.CreateAsync(new IdentityRole(model.Role));
            }

            // Remove user from all current roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Add to new role
            await _userManager.AddToRoleAsync(user, model.Role);

            return Ok(new { message = $"Rol succesvol gewijzigd naar {model.Role}!" });
        }

        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return token;
        }

		[HttpGet("debug-status")]
		public async Task<IActionResult> GetDebugStatus()
		{
			var debugInfo = new StringBuilder();
			debugInfo.AppendLine("--- Debug Start ---");

			// 1. Check JWT Config (zonder de geheime sleutel te tonen)
			debugInfo.AppendLine($"Issuer: {_configuration["Jwt:Issuer"]}");
			debugInfo.AppendLine($"Audience: {_configuration["Jwt:Audience"]}");
			debugInfo.AppendLine($"Key Length: {_configuration["Jwt:Key"]?.Length ?? 0} chars");

			// 2. Check Database Verbinding
			try
			{
                _ = _userManager.Users.Take(1).ToList();
                bool canConnect = true;
				debugInfo.AppendLine($"Database Verbinding: {(canConnect ? "SUCCESS" : "FAILED")}");
			}
			catch (Exception ex)
			{
				debugInfo.AppendLine($"Database Verbinding Error: {ex.Message}");
			}

			// 3. Check of Tabellen bestaan (AspNetUsers is de belangrijkste)
			try
			{
				var userCount = _userManager.Users.Count();
				debugInfo.AppendLine($"Aantal gebruikers in DB: {userCount}");
			}
			catch (Exception ex)
			{
				debugInfo.AppendLine($"Tabel Check Error (AspNetUsers): {ex.Message}");
			}

			return Ok(debugInfo.ToString());
		}
	}

    public class LoginModel
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class RegisterModel
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
    public class UpdateRoleModel
    {
        public required string Role { get; set; }
    }
}

