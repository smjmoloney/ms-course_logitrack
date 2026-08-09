using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ms_course_logitrack.Auth;

namespace ms_course_logitrack.Controllers
{
	[ApiController]
	[Route("api/auth")]
	public class AuthController : ControllerBase
	{
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly IConfiguration _configuration;

		public AuthController(
			UserManager<ApplicationUser> userManager,
			SignInManager<ApplicationUser> signInManager,
			IConfiguration configuration)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_configuration = configuration;
		}

		[HttpPost("register")]
		public async Task<IActionResult> Register(RegisterRequest request)
		{
			var user = new ApplicationUser
			{
				UserName = request.Username,
				Email = request.Email
			};

			var result = await _userManager.CreateAsync(user, request.Password);

			if (!result.Succeeded)
			{
				return BadRequest(new
				{
					errors = result.Errors.Select(error => error.Description)
				});
			}

			var roleResult = await _userManager.AddToRoleAsync(user, "User");

			if (!roleResult.Succeeded)
			{
				await _userManager.DeleteAsync(user);
				return StatusCode(StatusCodes.Status500InternalServerError, new
				{
					errors = roleResult.Errors.Select(error => error.Description)
				});
			}

			return StatusCode(StatusCodes.Status201Created, new
			{
				user.Id,
				user.UserName,
				user.Email
			});
		}

		[HttpPost("login")]
		public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
		{
			var user = await _userManager.FindByNameAsync(request.Username);

			if (user == null)
			{
				return Unauthorized(new ProblemDetails
				{
					Status = StatusCodes.Status401Unauthorized,
					Title = "Login failed",
					Detail = "The username or password is incorrect."
				});
			}

			var signInResult = await _signInManager.CheckPasswordSignInAsync(
				user,
				request.Password,
				lockoutOnFailure: true);

			if (!signInResult.Succeeded)
			{
				return Unauthorized(new ProblemDetails
				{
					Status = StatusCodes.Status401Unauthorized,
					Title = "Login failed",
					Detail = "The username or password is incorrect."
				});
			}

			var expiresAt = DateTime.UtcNow.AddHours(1);
			var token = await CreateToken(user, expiresAt);

			return Ok(new AuthResponse(token, expiresAt));
		}

		private async Task<string> CreateToken(ApplicationUser user, DateTime expiresAt)
		{
			var signingKey = _configuration["Jwt:Key"]
				?? throw new InvalidOperationException("JWT signing key is not configured.");
			var claims = new List<Claim>
			{
				new Claim(JwtRegisteredClaimNames.Sub, user.Id),
				new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName!),
				new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
			};
			var roles = await _userManager.GetRolesAsync(user);
			claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
			var credentials = new SigningCredentials(
				new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
				SecurityAlgorithms.HmacSha256);
			var token = new JwtSecurityToken(
				issuer: _configuration["Jwt:Issuer"],
				audience: _configuration["Jwt:Audience"],
				claims: claims,
				expires: expiresAt,
				signingCredentials: credentials);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}
	}
}
