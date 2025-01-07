using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TeleMedicineApp.Data;
using TeleMedicineApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace TeleMedicineApp.Services
{
    public interface IJwtService
    {
        string GenerateToken(ApplicationUser user, IList<string> roles);
        Task<(string token, string refreshToken)> GenerateTokensAsync(ApplicationUser user, IList<string> roles);
        Task<(string token, string refreshToken)> RefreshTokenAsync(string refreshToken);
    }

    public class JwtService : IJwtService
    {
        private readonly JwtConfig _jwtConfig;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public JwtService(
            JwtConfig jwtConfig,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _jwtConfig = jwtConfig;
            _context = context;
            _userManager = userManager;
        }

        public string GenerateToken(ApplicationUser user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddMinutes(_jwtConfig.ExpirationInMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtConfig.Issuer,
                audience: _jwtConfig.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<(string token, string refreshToken)> GenerateTokensAsync(ApplicationUser user, IList<string> roles)
        {
            var jwtToken = GenerateToken(user, roles);
            var refreshToken = await GenerateRefreshTokenAsync(user.Id);

            return (jwtToken, refreshToken);
        }

        public async Task<(string token, string refreshToken)> RefreshTokenAsync(string refreshToken)
        {
            var storedRefreshToken = await _context.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Token == refreshToken);

            if (storedRefreshToken == null)
                throw new SecurityTokenException("Invalid refresh token");

            if (storedRefreshToken.ExpiryDate < DateTime.Now)
                throw new SecurityTokenException("Refresh token expired");

            if (storedRefreshToken.Used)
                throw new SecurityTokenException("Refresh token already used");

            if (storedRefreshToken.Invalidated)
                throw new SecurityTokenException("Refresh token invalidated");

            // Mark current refresh token as used
            storedRefreshToken.Used = true;
            _context.RefreshTokens.Update(storedRefreshToken);

            var user = storedRefreshToken.User;
            var roles = await _userManager.GetRolesAsync(user);

            // Generate new tokens
            var newJwtToken = GenerateToken(user, roles);
            var newRefreshToken = await GenerateRefreshTokenAsync(user.Id);

            await _context.SaveChangesAsync();

            return (newJwtToken, newRefreshToken);
        }

        private async Task<string> GenerateRefreshTokenAsync(string userId)
        {
            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                JwtId = Guid.NewGuid().ToString(),
                CreationDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(7), // Refresh token valid for 7 days
                Used = false,
                Invalidated = false,
                UserId = userId
            };

            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            return refreshToken.Token;
        }
    }
}
