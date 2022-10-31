using ExampleAPI;
using Microsoft.IdentityModel.Tokens;
using PrettyNeat.Keycloak.Helpers;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//get configurator
var aot_conf = builder.Configuration as IConfigurationRoot;

//add permissive-ish cors options for the frontend
builder.Services.AddCors(opts =>
{
    opts.AddPolicy("vue3",
        pol =>
    {
        pol.AllowAnyHeader();
        pol.AllowAnyMethod();
        pol.AllowCredentials();
        pol.WithOrigins("http://127.0.0.1:5173");
        pol.Build();
    });
});

//add the Authentication and Authorization suite
builder.Services.AddAuthentication(opts =>
{
    opts.DefaultScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
              opts =>
              {
                  opts.TokenValidationParameters =
                  new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                  {
                      ValidateActor = false,                      
                      ValidateAudience = false,
                      ValidIssuer = aot_conf["Jwt:issuer"],
                      IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(aot_conf["Jwt:secret"]))
                  };

                  opts.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                  {
                      OnForbidden = (ctx) =>
                      {
                          return Task.CompletedTask;
                      },
                      OnAuthenticationFailed = (ctx) =>
                      {
                          return Task.CompletedTask;
                      },
                      OnChallenge = (ctx) =>
                      {
                          return Task.CompletedTask;
                      }
                  };
              });

builder.Services.AddAuthorization();

builder.Services.AddKeyCloakProvider();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//this middleware HAS to run before AuthN/AuthZ
app.UseCors("vue3");

app.UseAuthentication();
app.UseAuthorization();


var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateTime.Now.AddDays(index),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.RequireAuthorization()
.RequireCors("vue3");

app.MapGet("/oauth", OauthEndpoint.Process)
    .AllowAnonymous()
    .RequireCors("vue3");

app.Urls.Clear();
app.Urls.Add("http://localhost:5000");

app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}