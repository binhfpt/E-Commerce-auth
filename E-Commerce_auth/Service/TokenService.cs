using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using E_Commerce_auth.Enum;
using E_Commerce_auth.Models;
using Microsoft.IdentityModel.Tokens;

namespace E_Commerce_auth.Service

{
    public class TokenService
    {
        IConfiguration configuration;
        public TokenService(IConfiguration config)
        {
            configuration = config;
        }
        

        public string GenerateAccessToken(User user, string sid)
        {
            var issuer = configuration["JwtConfig:Issuer"];
            var audience = configuration["JwtConfig:Audience"];
            var key = configuration["JwtConfig:AccessKey"];
            var accessMins =  configuration.GetValue<int>("JwtConfig:AccessTokenExMins");
            //var refreshMins = configuration.GetValue<int>("JwtConfig:RefreshTokenExMins");
            // ma hoa key
            var hashKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!));
            // dung key nao de ki ? dung thuat toan ma hoa nao cho jwt
            var credentials = new SigningCredentials(hashKey, SecurityAlgorithms.HmacSha256);
            // khoi tao description token ( payload )
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("uid", user.UserId + ""),
                    new Claim("role",user.Role),
                    new Claim("sid",sid),
                    new Claim("jti",Guid.NewGuid().ToString("N")),
            
                }),
                Expires = DateTime.UtcNow.AddMinutes(accessMins),
                //Issuer = issuer,
                //Audience = audience,
                SigningCredentials = credentials
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        public string GenerateAccessToken(User user, int tokenVersion, string sid)
        {
            var issuer = configuration["JwtConfig:Issuer"];
            var audience = configuration["JwtConfig:Audience"];
            var key = configuration["JwtConfig:AccessKey"];
            var accessMins = configuration.GetValue<int>("JwtConfig:AccessTokenExMins");
            //var refreshMins = configuration.GetValue<int>("JwtConfig:RefreshTokenExMins");
            // ma hoa key
            var hashKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!));
            // dung key nao de ki ? dung thuat toan ma hoa nao cho jwt
            var credentials = new SigningCredentials(hashKey, SecurityAlgorithms.HmacSha256);
            // khoi tao description token ( payload )
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("uid", user.UserId + ""),
                    new Claim("role",user.Role),
                    new Claim("tv", tokenVersion + ""),
                    new Claim("sid",sid),
                    new Claim("jti",Guid.NewGuid().ToString("N")),


                }),
                Expires = DateTime.UtcNow.AddMinutes(accessMins),
                //Issuer = issuer,
                //Audience = audience,
                SigningCredentials = credentials
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        public string GenerateRefreshToken(User user,string sid)
        {
            var issuer = configuration["JwtConfig:Issuer"];
            var audience = configuration["JwtConfig:Audience"];
            var key = configuration["JwtConfig:RefreshKey"];
            //var accessMins = configuration.GetValue<int>("JwtConfig:AccessTokenExMins");
            var refreshMins = configuration.GetValue<int>("JwtConfig:RefreshTokenExMins");
            // ma hoa key
            var hashKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!));
            // dung key nao de ki ? dung thuat toan ma hoa nao cho jwt
            var credentials = new SigningCredentials(hashKey, SecurityAlgorithms.HmacSha256);
            // khoi tao description token ( payload )
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("uid", user.UserId + ""),
                    new Claim("role",user.Role),
                    new Claim("sid",sid),
                    new Claim("jti",Guid.NewGuid().ToString("N")),


                }),
                Expires = DateTime.UtcNow.AddMinutes(refreshMins),
                //Issuer = issuer,
                //Audience = audience,
                SigningCredentials = credentials
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        public string GenerateRefreshToken(User user, int tokenVersion,string sid)
        {
            var issuer = configuration["JwtConfig:Issuer"];
            var audience = configuration["JwtConfig:Audience"];
            var key = configuration["JwtConfig:RefreshKey"];
            //var accessMins = configuration.GetValue<int>("JwtConfig:AccessTokenExMins");
            var refreshMins = configuration.GetValue<int>("JwtConfig:RefreshTokenExMins");
            // ma hoa key
            var hashKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!));
            // dung key nao de ki ? dung thuat toan ma hoa nao cho jwt
            var credentials = new SigningCredentials(hashKey, SecurityAlgorithms.HmacSha256);
            // khoi tao description token ( payload )
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("uid", user.UserId + ""),
                    new Claim("role",user.Role),
                    new Claim("tv", tokenVersion + ""),
                    new Claim("sid",sid),
                    new Claim("jti",Guid.NewGuid().ToString("N")),

                }),
                Expires = DateTime.UtcNow.AddMinutes(refreshMins),
                //Issuer = issuer,
                //Audience = audience,
                SigningCredentials = credentials
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        public ClaimsPrincipal? ValidateToken(string token, TokenType tokenType)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            //var accessKey = configuration["JwtConfig:AccessKey"];
            //var refreshKey = configuration["JwtConfig:RefreshKey"];
            string? secret = tokenType switch
            {
                TokenType.Access => configuration["JwtConfig:AccessKey"],
                TokenType.Refresh => configuration["JwtConfig:RefreshKey"],
                _ => null
            };
            if (string.IsNullOrWhiteSpace(secret)) return null;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var handler = new JwtSecurityTokenHandler();

            try
            {
                var principal = handler.ValidateToken(
                    token,
                    new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = key,

                        ValidateIssuer = false,    
                        ValidateAudience = false,  

                        ValidateLifetime = true,   // check exp
                        ClockSkew = TimeSpan.Zero  // hết hạn là chết luôn
                    },
                    out var validatedToken
                );

                // Optional: chặn thuật toán lạ (nên có)
                if (validatedToken is not JwtSecurityToken jwt ||
                    !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}
