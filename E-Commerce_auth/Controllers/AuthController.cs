using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Azure.Core;
using E_Commerce_auth.DTO;
using E_Commerce_auth.Models;
using E_Commerce_auth.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace E_Commerce_auth.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : Controller
    {
        AuthService auth_service;
        RedisService redisService;
        TokenService tokenService;
        IConfiguration _configuration;

        public AuthController(AuthService auth_service, RedisService redisService, IConfiguration configuration, TokenService tokenService)
        {
            this.auth_service = auth_service;
            this.redisService = redisService;
            _configuration = configuration;
            this.tokenService = tokenService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest rq)
        {   
            string username = rq.UserName;
            string password = rq.Password;
            var user = await auth_service.Login(username, password);
            if (user == null)
            {
                return Unauthorized("Invalid username or password");
            }
            string sid = Guid.NewGuid().ToString("N");
            var accessMins = _configuration.GetValue<int>("JwtConfig:AccessTokenExMins");
            var refreshMins = _configuration.GetValue<int>("JwtConfig:RefreshTokenExMins");
            if (user.Role == "customer")
            {
                var RefreshUserToken = tokenService.GenerateRefreshToken(user,sid);
                var AccessUserToken = tokenService.GenerateAccessToken(user, sid);
                await redisService.SetAsync($"rt:{user.UserId}:{sid}", RefreshUserToken, TimeSpan.FromMinutes(refreshMins));
                SetAuthCookies(AccessUserToken, RefreshUserToken, accessMins, refreshMins);

            }
            else if(user.Role == "staff")
            {   
                // kiem tra xem co version chua
                var existedVersion = await redisService.GetAsync($"tv:{user.UserId}");
                int version = 0;
                // chua co thi them, defaul val = 0
                if(existedVersion == null)
                {
                    await redisService.SetAsync($"tv:{user.UserId}", "0");
                }
                else version = int.Parse(existedVersion);

                // tao pair token 
                var refreshStaffToken = tokenService.GenerateRefreshToken(user, version,sid);
                var accessStaffToken = tokenService.GenerateAccessToken(user, version,sid);

                // them vao redis
                await redisService.SetAsync($"rt:{user.UserId}:{sid}", refreshStaffToken, TimeSpan.FromMinutes(refreshMins));
                SetAuthCookies(accessStaffToken, refreshStaffToken, accessMins, refreshMins);


            }
            return Ok(user);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] LoginRequest rq)
        {   
            string username = rq.UserName;
            string password = rq.Password;
            var user = await auth_service.Register(username, password);
            if (user == null)
            {
                return BadRequest("User already exists or invalid data");
            }
            return Ok(user);
        }
        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            //redisService.SetAsync("health_check", "ok", TimeSpan.FromMinutes(1));

            return Ok("Auth-Called");
        }
        //[HttpGet("health2")]
        //public async Task<IActionResult> Health2()
        //{
        //    var x =await redisService.GetAsync("health_check");

        //    return Ok(x);
        //}

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            var rt = Request.Cookies["refresh_token"];
            if (string.IsNullOrWhiteSpace(rt)) return Unauthorized();

            // validate token xem chu ki ok ko va con exp ko
            var principal = tokenService.ValidateToken(rt,Enum.TokenType.Refresh);
            if (principal == null) return Unauthorized();

            // lay payload data
            var userId = principal.FindFirst("uid")?.Value;
            var sid = principal.FindFirst("sid")?.Value;
            var role = principal.FindFirst("role")?.Value;

            if (userId == null || sid == null || role == null) return Unauthorized();
            // tim refresh token trong redis
            var redisKey = $"rt:{userId}:{sid}";
            var rtInRedis = await redisService.GetAsync(redisKey);
            if (rtInRedis == null || rtInRedis != rt) return Unauthorized();

            // staff: check token_version
            int version = 0;
            if (role == "staff")
            {
                var redisVer = await redisService.GetAsync($"tv:{userId}") ?? "0";
                version = int.Parse(redisVer);

                var tokenVer = principal.FindFirst("tv")?.Value ?? "0";
                if (tokenVer != version.ToString()) return Unauthorized();
            }

            // rotate + set cookie mới
            var refreshMins = int.Parse(_configuration["JwtConfig:RefreshTokenExMins"]!);
            var accessMins = int.Parse(_configuration["JwtConfig:AccessTokenExMins"]!);

            var user = new User { UserId = int.Parse(userId), Role = role };

            var newRt = role == "staff"
                ? tokenService.GenerateRefreshToken(user, version, sid)
                : tokenService.GenerateRefreshToken(user, sid);

            var newAt = role == "staff"
                ? tokenService.GenerateAccessToken(user, version, sid)
                : tokenService.GenerateAccessToken(user, sid);

            await redisService.SetAsync(redisKey, newRt, TimeSpan.FromMinutes(refreshMins));
            SetAuthCookies(newAt, newRt, accessMins, refreshMins);

            return Ok();
        }

        public async Task<IActionResult> BanStaff([FromBody] int userId)
        {
            var newIndex = await redisService.IncrementAsync($"tv:{userId}");
            return Ok(new
            {
                userId,
                tokenVersion = newIndex
            });
        }




        private void SetAuthCookies(string accessToken, string refreshToken, int accessMins, int refreshMins)
        {
            var cookieOptionsAccess = new CookieOptions
            {
                HttpOnly = true,
                Secure = false,          // local dev http thì để false (true tuc la chi https moi gui kem cookie)
                SameSite = SameSiteMode.None, // nếu frontend khác domain -> dùng None + Secure
                Expires = DateTimeOffset.UtcNow.AddMinutes(accessMins),
                Path = "/"
            };

            var cookieOptionsRefresh = new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddMinutes(refreshMins),
                Path = "/auth/refresh"  // refresh chỉ gửi khi gọi refresh API (giảm rủi ro)
            };

            Response.Cookies.Append("access_token", accessToken, cookieOptionsAccess);
            Response.Cookies.Append("refresh_token", refreshToken, cookieOptionsRefresh);
        }


    }
}
