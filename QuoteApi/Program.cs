using Microsoft.EntityFrameworkCore;
using QuoteApi.Data;

namespace QuoteApi;

// DO NOT remove the Program class declaration or the Main method. These are needed for the tests.

// DO edit the content of Main method to handle the task requirements.
public class Program
{
    public static void Main(string[] args)
    {

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        var connectionString = builder.Configuration.GetConnectionString("QuoteDb");
        builder.Services.AddDbContext<QuoteContext>(options =>
            options.UseSqlite(connectionString));
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

        //Add initial data to the database when the app starts running
        using (var serviceScope = app.Services.CreateScope())
        {
            var services = serviceScope.ServiceProvider;
            InitializeTheDatabase(app.Environment, services.GetRequiredService<QuoteContext>());
        }

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

        void InitializeTheDatabase(IWebHostEnvironment env, QuoteContext db)
        {
            if (env.IsDevelopment())
            {
                db.Database.EnsureCreated(); // Create the database and tables
                if (db.Quotes.Count() == 0)
                {
                    // Add some test data
                    db.Quotes.Add(new Quote
                    {
                        Id = 22,
                        TheQuote = "Thinking is the hardest work there is, which is the probable reason why so few engage in it.",
                        WhoSaid = "Henry Ford",
                        WhenWasSaid = DateTime.Parse("1922-07-01"),
                        QuoteCreator = "Violet",
                        QuoteCreateDate = DateTime.Parse("2020-11-22")
                    });
                    db.Quotes.Add(new Quote
                    {
                        Id = 33,
                        TheQuote = "Imagination is more important than knowledge.",
                        WhoSaid = "Albert Einstein",
                        WhenWasSaid = DateTime.Parse("1970-05-01"),
                        QuoteCreator = "Butterfly",
                        QuoteCreateDate = DateTime.Parse("2022-03-31")
                    });
                    db.Quotes.Add(new Quote
                    {
                        Id = 44,
                        TheQuote = "The time is always right to do what is right.",
                        WhoSaid = "Martin Luther King, Jr",
                        WhenWasSaid = DateTime.Parse("1985-03-01"),
                        QuoteCreator = "Arthur",
                        QuoteCreateDate = DateTime.Parse("2021-06-15")
                    });
                    db.Quotes.Add(new Quote
                    {
                        Id = 55,
                        TheQuote = "Anything’s possible if you’ve got have enough nerve.",
                        WhoSaid = "J.K. Rowling",
                        WhenWasSaid = DateTime.Parse("2003-11-01"),
                        QuoteCreator = "Violet",
                        QuoteCreateDate = DateTime.Parse("2021-12-23")
                    });

                    db.SaveChanges();
                }
            }
        }
    }
}