using Microsoft.Azure.Cosmos;
using StockMarketApp.Scraper.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

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

var host = builder.Build();


host.Run();
