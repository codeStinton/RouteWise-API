using RouteWise.Middleware;
using RouteWise.Models.Amadeus;
using RouteWise.Services;
using RouteWise.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5002);
    options.ListenAnyIP(5003, listenOptions =>
        listenOptions.UseHttps());
});

builder.Services.Configure<AmadeusSettings>(builder.Configuration.GetSection("Amadeus"));
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IAuthentication, Authentication>();
builder.Services.AddHttpClient<IFlightSearchServiceV1, FlightSearchServiceV1>();
builder.Services.AddHttpClient<IFlightSearchServiceV2, FlightSearchServiceV2>();
builder.Services.AddHttpClient<IMultiCityServiceV2, MultiCityServiceV2>();

builder.Services.AddSingleton<IAuthentication, Authentication>();

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

app.UseExceptionHandlingMiddleware();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();