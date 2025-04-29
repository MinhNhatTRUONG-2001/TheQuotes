using Microsoft.EntityFrameworkCore;
using QuoteApi.Data;
using Npgsql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DotNetEnv;
using QuoteApi.Services.Email;
using System.Threading.RateLimiting;

Env.Load(); // Load environment variables from .env file

var builder = WebApplication.CreateBuilder(args);

// Load SMTP settings from configuration
builder.Services.Configure<EmailSettings>(settings =>
{
    builder.Configuration.GetSection("SmtpSettings").Bind(settings);
    settings.Host = Environment.GetEnvironmentVariable("MAIL_SERVER_HOST") ?? "";
    settings.Port = int.Parse(Environment.GetEnvironmentVariable("MAIL_SERVER_PORT") ?? "0");
    settings.UserName = Environment.GetEnvironmentVariable("MAIL_SERVER_USERNAME") ?? "";
    settings.Password = Environment.GetEnvironmentVariable("MAIL_SERVER_PASSWORD") ?? "";
});
builder.Services.AddTransient<IEmailService, EmailService>();

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
connectionStringBuilder.Database = Environment.GetEnvironmentVariable("DATABASE_NAME");
connectionStringBuilder.Username = Environment.GetEnvironmentVariable("DATABASE_USERNAME");
connectionStringBuilder.Password = Environment.GetEnvironmentVariable("DATABASE_PASSWORD");
var connectionString = connectionStringBuilder.ConnectionString;
builder.Services.AddDbContext<QuoteContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins(Environment.GetEnvironmentVariable("CLIENT_URL") ?? "")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 20,
                QueueLimit = 5,
                Window = TimeSpan.FromSeconds(5),
            }));
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
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