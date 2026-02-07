using E_Commerce_auth.Models;
using E_Commerce_auth.Repo;
using Microsoft.IdentityModel.Tokens;

namespace E_Commerce_auth.Service
{
    public class AuthService
    {
        AuthRepo _authRepo;
        public AuthService(AuthRepo auth)
        {
            _authRepo = auth;
        }
        public async Task<User?> Login(string name,string password)
        {
            if (name.IsNullOrEmpty()) return null;
            if(password.IsNullOrEmpty()) return null;
            return await _authRepo.ExistedUser(name, password);
        }

        public async Task<User?> Register(string name,string password)
        {
            if (name.IsNullOrEmpty()) return null;
            if (password.IsNullOrEmpty()) return null;
            var existedUser = await _authRepo.ExistedUser(name, password);
            if (existedUser != null) return null;
            return await _authRepo.Registry(name, password);
        }

       



    }
}
