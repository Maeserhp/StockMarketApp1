using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Cosmos;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StockMarketApp.APIWorker;
using StockMarketApp.APIWorker.Authentication;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<Worker>();
//builder.Services.AddHostedService<Worker>();

builder.Services.AddSingleton<CosmosClient>(serviceProvider =>
{
    var endpoint = builder.Configuration["CosmosDB:Endpoint"];
    var authKey = builder.Configuration["CosmosDB:PrimaryKey"];

    if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(authKey))
    {
        throw new ArgumentNullException("Cosmos DB endpoint or auth key not found in configuration.");
    }
    var cosmosClientOptions = new CosmosClientOptions
    {
        // Add any specific options here, e.g., ConnectionMode
        ConnectionMode = ConnectionMode.Gateway
    };

    return new CosmosClient(endpoint, authKey, cosmosClientOptions);

});

builder.Services.AddHttpClient();

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"], // The app setting
        ValidAudience = builder.Configuration["Jwt:Audience"], // The app setting
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])) // The secret key
    };
});

// Add CORS policy for your Angular app
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins("http://localhost:4200", "https://stockmarketappcapstone-baduapd7dkcdcah2.canadacentral-01.azurewebsites.net") // Change to your Angular app's URL
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();
app.UseCors(); // Use the CORS policy

app.UseAuthentication(); // Must be before app.UseAuthorization()
app.UseAuthorization();

app.MapControllers();

app.Run();
