using ei_back.Infrastructure.Exceptions.ExceptionTypes;
﻿using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ei_back.Infrastructure.Token
{
    public interface ITokenService
    {
        string GenerateAccessToken(IEnumerable<Claim> claims);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }

    public class TokenService : ITokenService
    {
        private TokenConfiguration _tokenConfiguration;

        public TokenService(TokenConfiguration tokenConfiguration)
        {
            _tokenConfiguration = tokenConfiguration;
        }

        public string GenerateAccessToken(IEnumerable<Claim> claims)
        {
            //Generate the simetric key
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenConfiguration.Secret));
            var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var options = new JwtSecurityToken(
                issuer: _tokenConfiguration.Issuer,
                audience: _tokenConfiguration.Audience,
                claims: claims,
                expires: DateTimeOffset.UtcNow.AddMinutes(_tokenConfiguration.Minutes).UtcDateTime,
                signingCredentials: signinCredentials
            );

            //Generate the token
            string tokenString = new JwtSecurityTokenHandler().WriteToken(options);
            return tokenString;
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudience = _tokenConfiguration.Audience,
                ValidateIssuer = true,
                ValidIssuer = _tokenConfiguration.Issuer,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenConfiguration.Secret)),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;

            ClaimsPrincipal principal;
            try
            {
                principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            }
            catch (Exception exception) when (exception is SecurityTokenException or ArgumentException)
            {
                throw new UnauthorizedException("Invalid credentials.");
            }
            var jwtSecurityToken  = securityToken as JwtSecurityToken;

            if (
                jwtSecurityToken == null ||
                !jwtSecurityToken.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCulture
                )
            ) throw new UnauthorizedException("Invalid credentials.");

            return principal;
        }
    }
}
