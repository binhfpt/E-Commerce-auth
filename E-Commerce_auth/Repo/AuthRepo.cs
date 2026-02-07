using E_Commerce_auth.Models;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
namespace E_Commerce_auth.Repo
{
    public class AuthRepo
    {
        Prn22FinalProjectContext _context;
        public AuthRepo(Prn22FinalProjectContext context)
        {
            _context = context;
        }
        public async Task<User?> ExistedUser(string username, string password)
        {
            // find user by username
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return null;

            // verify password
            bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!isValid) return null;

            return user;
        }
        public async Task<User> Registry(string username, string password)
        {
            // hash password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            var newUser = new User
            {
                Username = username,
                FullName = username,
                PasswordHash = hashedPassword,
                Role = "customer",
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            await _context.Users.AddAsync(newUser);
            await _context.SaveChangesAsync();
            return newUser;
        }



    }
}
