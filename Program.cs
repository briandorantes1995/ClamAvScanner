using ClamScanner.Configurations;
using ClamScanner.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<Clamd>();
builder.Services.AddRateLimits();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseRateLimiter();

app.MapPost("/scan", async (IFormFile file, Clamd clamAv) =>
{
    using var stream = file.OpenReadStream();

    bool clean = await clamAv.IsCleanAsync(stream);

    return clean
        ? Results.Ok("Archivo limpio")
        : Results.BadRequest("Archivo infectado");
    
}).RequireRateLimiting("public").DisableAntiforgery();

app.Run();

