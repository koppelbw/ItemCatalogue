using Application.ServicePorts;
using Microsoft.AspNetCore.Mvc;

namespace ItemCatalogueAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController(IItemService itemService) : ControllerBase
    {
        private static readonly string[] Summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get(CancellationToken cancellationToken)
        {
            var result = await itemService.GetByIdAsync(1, cancellationToken);

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = result.Description
            })
            .ToArray();
        }
    }
}
