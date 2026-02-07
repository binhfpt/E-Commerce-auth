namespace E_Commerce_auth

{
    using E_Commerce_auth.Models;
    using E_Commerce_auth.Repo;
    using E_Commerce_auth.Service;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using StackExchange.Redis;

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);          
            builder.Services.AddControllers();

            builder.Services.AddDbContext<Prn22FinalProjectContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DBDefault"))
);
            /////////////////////////////////////////////////////// Redis Configuration Begin ///////////////////////////////////////////////////
            // tao 1 redis connection duy nhat
            builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")));          
            // tao 1 doi tuong ket noi den redis
            builder.Services.AddSingleton<IDatabase>(sp =>
                sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase()
            );
            /////////////////////////////////////////////////////// Redis Configuration End ///////////////////////////////////////////////////

            /////////////////////////////////////////////////////// DI begin /////////////////////////////////////////////////////
            
            builder.Services.AddScoped<AuthRepo>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<TokenService>();
            builder.Services.AddScoped<RedisService>();
            /////////////////////////////////////////////////////// DI begin /////////////////////////////////////////////////////

            var app = builder.Build();
            //app.Logger.LogInformation(jwtSecret);
            app.MapControllers();

            app.Run();
        }
    }
}
