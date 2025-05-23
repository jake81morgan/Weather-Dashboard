using System.Net.Http;
using System.Text.Json;

namespace WeatherDashboard
{
    class WeatherService
    {

        private readonly string apiKey = "d913d7eef07833fcaeb90240a0af0489";

        private readonly HttpClient _httpClient;

        public WeatherService()
        {
            _httpClient = new HttpClient();
        }

        // Gets the weather data for a given city
        public async Task<string> GetWeather(string city)
        {
            try
            {
                var (lat, lon) = await GetCoordinatesAsync(city);
                if (lat == null || lon == null)
                {
                    return "Error: City not found.";
                }

                string weatherJson = await GetWeatherJsonAsync(lat.Value, lon.Value);
                return weatherJson;
            }
            catch (HttpRequestException ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        // Gets the coordinates (latitude and longitude) for a given city
        private async Task<(double? lat, double? lon)> GetCoordinatesAsync(string city)
        {
            string geoUrl = $"https://api.openweathermap.org/geo/1.0/direct?q={city}&limit=1&appid={apiKey}";
            string geoJson = await _httpClient.GetStringAsync(geoUrl);

            var geoData = Newtonsoft.Json.Linq.JArray.Parse(geoJson);
            if (geoData.Count == 0)
                return (null, null);

            double lat = (double)geoData[0]["lat"];
            double lon = (double)geoData[0]["lon"];
            return (lat, lon);
        }

        // Gets the weather data in JSON format for a given latitude and longitude
        private async Task<string> GetWeatherJsonAsync(double lat, double lon)
        {
            string oneCallUrl = $"https://api.openweathermap.org/data/3.0/onecall?lat={lat}&lon={lon}&appid={apiKey}&units=metric";
            return await _httpClient.GetStringAsync(oneCallUrl);
        }

        // Parses the weather data from JSON format
        public JsonDocument ParseWeatherData(string rawJson)
        {
            return JsonDocument.Parse(rawJson);
        }
    }
}
