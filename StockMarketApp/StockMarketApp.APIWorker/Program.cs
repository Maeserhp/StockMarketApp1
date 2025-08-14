using Microsoft.Azure.Cosmos;
using StockMarketApp.APIWorker;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<Worker>();
//builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<CosmosClient>(serviceProvider =>
{
    var endpoint = builder.Configuration["CosmosDB:Endpoint"];
    var authKey = builder.Configuration["CosmosDb:PrimaryKey"];

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

app.UseAuthorization();

app.MapControllers();

app.Run();
