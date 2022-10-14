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
.RequireAuthorization();

app.MapGet("/oauth", OauthEndpoint.Process)
    .AllowAnonymous();

app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}