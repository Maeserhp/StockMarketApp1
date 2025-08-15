using Microsoft.Azure.Cosmos;
using Microsoft.OpenApi.Models;
using StockMarketApp.APIWorker;
using StockMarketApp.APIWorker.Authentication;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(x =>
{
    //Set up Swagger Api Key authorization
    x.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "The API Key to access the API",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Name = "x-api-key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Scheme = "ApiKeyScheme"
    });

    var scheme = new OpenApiSecurityScheme
    {
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "ApiKey"
        },
        In = ParameterLocation.Header
    };

    var requirement = new OpenApiSecurityRequirement
    {
        {scheme, new List<string>() }
    };
    x.AddSecurityRequirement(requirement);

});
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseHttpsRedirection();

app.UseMiddleware<ApiKeyAuthMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
