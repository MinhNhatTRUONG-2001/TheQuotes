using Microsoft.EntityFrameworkCore;
using QuoteApi.Data;
using Npgsql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace QuoteApi;

public class Program
{
    public static void Main(string[] args)
    {

        var builder = WebApplication.CreateBuilder(args);

        //JWT configuration
        string jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "";
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
         .AddJwtBearer(options =>
         {
             options.TokenValidationParameters = new TokenValidationParameters
             {
                 ValidateIssuer = false,
                 ValidateAudience = false,
                 ValidateIssuerSigningKey = true,
                 // Note: manually padding to 512 bits if it is a short key, as the SymmetricSignatureProvider does not do the HMACSHA512 RFC2104 padding for you.
                 IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey.PadRight(512 / 8, '\0'))),
                 ValidAlgorithms = [SecurityAlgorithms.HmacSha512]
             };
         });

        // Add services to the container.
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(
            builder.Configuration.GetConnectionString("QuoteDb"));
        connectionStringBuilder.Password = Environment.GetEnvironmentVariable("DATABASE_PASSWORD");
        var connectionString = connectionStringBuilder.ConnectionString;
        builder.Services.AddDbContext<QuoteContext>(options =>
            options.UseNpgsql(connectionString));
        builder.Services.AddCors(options => {
            options.AddDefaultPolicy(p => {
                p.AllowAnyHeader();
                p.AllowAnyMethod();
                p.AllowAnyOrigin();
            });
        });

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors();

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}