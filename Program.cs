using ClamScanner.Configurations;
using ClamScanner.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<Clamd>();
builder.Services.AddRateLimits();
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddHostedService<RabbitMqConsumerService>();

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
    if (file.Length == 0)
    {
        return Results.BadRequest(new { error = "No se proporcionó ningún archivo o está vacío." });
    }

    using var stream = file.OpenReadStream();

    
    ClamResult result = await clamAv.ScanAsync(stream);

    if (result.IsClean)
    {
        return Results.Ok(new
        {
            status = "clean",
            message = "Archivo limpio",
            fileName = file.FileName,
            size = file.Length
        });
    }
    
    return Results.UnprocessableEntity(new
    {
        status = "infected",
        message = "Archivo infectado",
        fileName = file.FileName,
        virusName = result.VirusName ?? "Amenaza Detectada", 
        rawResponse = result.RawResponse
    });
    
}).RequireRateLimiting("public").DisableAntiforgery();


app.Run();

