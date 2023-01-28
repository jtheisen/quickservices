using BlazorTestApp.Shared;

#pragma warning disable CS1998

namespace BlazorTestApp.Server.QuickServices;

public class WeatherForecastServiceImplementation : IWeatherForecastService
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastServiceImplementation> _logger;

    public WeatherForecastServiceImplementation(ILogger<WeatherForecastServiceImplementation> logger)
    {
        _logger = logger;
    }

    public async Task<WeatherForecast[]> GetForecast()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }

    public Task ThrowException()
    {
        throw new NotImplementedException();
    }
}