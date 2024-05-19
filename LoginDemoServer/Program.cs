using LoginDemoServer.Models;
using Microsoft.EntityFrameworkCore;

namespace LoginDemoServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            #region Add Database context to Dependency Injection
            //Read connection string from app settings.json
            string connectionString = builder.Configuration.GetSection("LoginDbConnectionString").Value;
            //Add Database to dependency injection
            builder.Services.AddDbContext<LoginDemoDbContext>(
                    options => options.UseSqlServer(connectionString));

            #endregion 

            #region Add Session
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromSeconds(3000);
                options.Cookie.HttpOnly = false;
                options.Cookie.IsEssential = true;
            });
            #endregion


            #region for debugginh UI
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            #endregion

            var app = builder.Build();

            #region for debugginh UI
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            #endregion
            app.UseHttpsRedirection();

            #region Add Session
            app.UseSession(); //In order to enable session management
            #endregion 

            app.MapControllers();
            app.UseStaticFiles();

            app.Run();
        }
    }
}