using Azure.Core;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();



builder.AddAzureBlobServiceClient("blobs",
    bs => {
        if (builder.Environment.IsDevelopment()) {
            bs.Credential = new AzureCliCredential();
        }
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
}

app.UseStaticFiles();
app.UseBlazorFrameworkFiles();

app.UseRouting();
app.UseAuthorization();
app.UseHealthChecks("/health");

app.MapFallbackToFile("index.html");

app.UseHttpsRedirection();

app.Run();