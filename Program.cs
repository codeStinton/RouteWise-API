using RouteWise.Models.Amadeus;
using RouteWise.Services;
using RouteWise.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AmadeusSettings>(builder.Configuration.GetSection("Amadeus"));

builder.Services.AddHttpClient();
builder.Services.AddScoped<IAmadeusService, AmadeusService>();
builder.Services.AddScoped<IAuthentication, Authentication>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();