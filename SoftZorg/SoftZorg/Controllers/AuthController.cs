using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SoftZorg.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        // We injecteren de UserManager (voor database checks) en IConfiguration (voor de JWT instellingen)
        public AuthController(UserManager<IdentityUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            // 1. Zoek de gebruiker op basis van e-mail
            var user = await _userManager.FindByEmailAsync(model.Email);

            // 2. Controleer of de gebruiker bestaat en het wachtwoord klopt
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                // 3. Haal de rollen op die bij deze specifieke gebruiker horen
                var userRoles = await _userManager.GetRolesAsync(user);

                // 4. Maak de "Claims" aan (dit is de data die we in de token stoppen)
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.UserName!),
                    new Claim(ClaimTypes.Email, user.Email!),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unieke ID voor de token
                };

                // Voeg voor elke rol die de gebruiker heeft een claim toe aan de token
                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                // 5. Genereer de daadwerkelijke token
                var token = GetToken(authClaims);

                // 6. Stuur de token en rollen terug naar de React frontend
                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo,
                    roles = userRoles // Handig voor React om meteen de interface aan te passen!
                });
            }

            // Als we hier komen, was het e-mailadres of wachtwoord fout
            return Unauthorized(new { message = "E-mailadres of wachtwoord is onjuist." });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            // 1. Check of het e-mailadres al in gebruik is
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
                return Conflict(new { message = "Er bestaat al een account met dit e-mailadres." });

            // 2. Maak de nieuwe gebruiker aan
            IdentityUser user = new()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Email // Identity eist een UserName, we gebruiken gewoon het e-mailadres
            };

            // 3. Sla de gebruiker op in de database met het wachtwoord (wordt automatisch gehasht!)
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                // ASP.NET Identity vereist standaard 1 hoofdletter, 1 cijfer, 1 speciaal teken en minimaal 6 tekens.
                return BadRequest(new { message = "Account aanmaken mislukt. Zorg voor een sterk wachtwoord (hoofdletter, cijfer, speciaal teken)." });
            }

            // 4. BELANGRIJK: Wijs de standaard rol "Verpleegkundige" toe!
            // Let op: De rol "Verpleegkundige" moet wel in de database bestaan, anders geeft dit een foutmelding.
            await _userManager.AddToRoleAsync(user, "Verpleegkundige");

            return Ok(new { message = "Account succesvol aangemaakt!" });
        }

        // Hulpmethode om de token in elkaar te zetten
        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            // Haal de geheime sleutel uit appsettings.json
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.Now.AddHours(3), // De token is 3 uur geldig
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return token;
        }
    }

    // Modellen die bepalen hoe de data vanuit React (JSON) eruit moet zien
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
}