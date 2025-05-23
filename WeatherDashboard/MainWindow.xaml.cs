using System.ComponentModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WeatherDashboard
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        private readonly WeatherService weatherService;

        // Property to toggle between Celsius and Fahrenheit
        private bool _isFahrenheit;
        public bool IsFahrenheit
        {
            get => _isFahrenheit;
            set
            {
                _isFahrenheit = value;
                OnPropertyChanged(nameof(IsFahrenheit));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            weatherService = new WeatherService();
            InitializeComponent();
        }

        // Event handler for the temperature toggle button
        private void ToggleTemperature_Click(object sender, RoutedEventArgs e)
        {
            IsFahrenheit = !IsFahrenheit;
            TempToggle.Content = IsFahrenheit ? "°F" : "°C";

            string city = CityBlock.Text;
            if (!string.IsNullOrWhiteSpace(city))
            {
                GetWeather_Click(null, null);
            }
        }

        // Event handler for the "Get Weather" button
        private async void GetWeather_Click(object sender, RoutedEventArgs e)
        {
            string city = CityInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(city))
            {
                CityInputError.Text = "Input a city name...";
                return;
            }

            try
            {
                CityInputError.Text = "";
                string response = await weatherService.GetWeather(city);

                using JsonDocument weatherDoc = JsonDocument.Parse(response);
                JsonElement weatherRoot = weatherDoc.RootElement;

                SetCurrentWeather(weatherRoot.GetProperty("current"), city);
                SetAirConditions(weatherRoot.GetProperty("current"));
                DisplayTodayForecast(weatherRoot.GetProperty("hourly"));
                DisplayForecast(weatherRoot.GetProperty("daily"));
            }
            catch (Exception ex)
            {
                CityInputError.Text = ex.Message.Contains("LineNumber: 0")
                    ? "City not found! Input a valid city name..."
                    : $"An error occurred: {ex.Message}";
            }
        }

        // Converts temperature from Celsius to Fahrenheit or vice versa
        private double ConvertTemperature(double tempInCelsius)
        {
            return IsFahrenheit ? (tempInCelsius * 9 / 5) + 32 : tempInCelsius;
        }

        // Returns the temperature symbol based on the selected unit
        private string TempSymbol()
        {
            return IsFahrenheit ? "°F" : "°C";
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Sets the current weather information in the UI
        private void SetCurrentWeather(JsonElement weatherCurrent, string city)
        {
            int humidity = weatherCurrent.GetProperty("humidity").GetInt32();
            double temperature = ConvertTemperature(weatherCurrent.GetProperty("temp").GetDouble());

            string iconCode = weatherCurrent.GetProperty("weather")[0].GetProperty("icon").GetString();
            string iconUrl = $"https://openweathermap.org/img/wn/{iconCode}@2x.png";

            WeatherIcon.Source = new BitmapImage(new Uri(iconUrl));
            CityBlock.Text = city;
            TempBlock.Text = $"{temperature:F1}{TempSymbol()}";
        }

        // Sets the air conditions information in the UI
        private void SetAirConditions(JsonElement weatherCurrent)
        {
            int realFeel = (int)ConvertTemperature(weatherCurrent.GetProperty("feels_like").GetDouble());
            int uvIndex = (int)weatherCurrent.GetProperty("uvi").GetDouble();
            double windSpeed = weatherCurrent.GetProperty("wind_speed").GetDouble();
            int clouds = weatherCurrent.GetProperty("clouds").GetInt32();

            RealFeel.Text = $"{realFeel}{TempSymbol()}";
            UV.Text = uvIndex.ToString();
            WindSpeed.Text = $"{windSpeed:F1} m/s";
            Clouds.Text = $"{clouds} %";
        }

        // Displays the hourly forecast for today
        private void DisplayTodayForecast(JsonElement weatherHourly)
        {
            TodayPanel.Children.Clear();

            for (int i = 0; i <= 18; i += 3)
            {
                JsonElement hour = weatherHourly[i];
                DateTime time = DateTimeOffset.FromUnixTimeSeconds(hour.GetProperty("dt").GetInt64()).DateTime;
                int temp = (int)ConvertTemperature(hour.GetProperty("temp").GetDouble());

                string iconCode = hour.GetProperty("weather")[0].GetProperty("icon").GetString();
                string iconUrl = $"https://openweathermap.org/img/wn/{iconCode}@2x.png";

                StackPanel column = new()
                {
                    Margin = new Thickness(0, 10, 0, 0),
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 80,
                };

                column.Children.Add(new TextBlock
                {
                    Text = time.ToString("h:mm tt"),
                    Foreground = Brushes.Gray,
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                column.Children.Add(new Image
                {
                    Source = new BitmapImage(new Uri(iconUrl)),
                    Height = 64,
                    Width = 64,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                column.Children.Add(new TextBlock
                {
                    Text = $"{temp}{TempSymbol()}",
                    Foreground = Brushes.White,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                TodayPanel.Children.Add(column);
            }
        }

        // Displays the 7-day forecast
        private void DisplayForecast(JsonElement weatherDaily)
        {
            ForecastPanel.Children.Clear();

            for (int i = 0; i < 7; i++)
            {
                JsonElement day = weatherDaily[i];
                DateTime date = DateTimeOffset.FromUnixTimeSeconds(day.GetProperty("dt").GetInt64()).DateTime;

                int minTemp = (int)ConvertTemperature(day.GetProperty("temp").GetProperty("min").GetDouble());
                int maxTemp = (int)ConvertTemperature(day.GetProperty("temp").GetProperty("max").GetDouble());

                string weatherDesc = day.GetProperty("weather")[0].GetProperty("main").GetString();
                string iconCode = day.GetProperty("weather")[0].GetProperty("icon").GetString();
                string iconUrl = $"https://openweathermap.org/img/wn/{iconCode}@2x.png";

                StackPanel row = new()
                {
                    Orientation = Orientation.Horizontal,
                    Width = 320,
                    Height = 53
                };

                row.Children.Add(new TextBlock
                {
                    Text = i == 0 ? "Today" : date.ToString("ddd"),
                    Foreground = Brushes.Gray,
                    FontSize = 14,
                    Width = 40,
                    VerticalAlignment = VerticalAlignment.Center
                });

                StackPanel middle = new()
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(40, 0, 0, 0),
                    Width = 183
                };

                middle.Children.Add(new Image
                {
                    Source = new BitmapImage(new Uri(iconUrl)),
                    Height = 64,
                    Width = 64
                });

                middle.Children.Add(new TextBlock
                {
                    Text = weatherDesc,
                    Foreground = Brushes.Gray,
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center
                });

                row.Children.Add(middle);

                row.Children.Add(new TextBlock
                {
                    Text = $"{maxTemp}/{minTemp}",
                    Foreground = Brushes.Gray,
                    FontSize = 14,
                    Width = 50,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Right
                });

                ForecastPanel.Children.Add(row);
                ForecastPanel.Children.Add(new Separator
                {
                    Margin = new Thickness(0, 5, 0, 5),
                    Background = Brushes.Gray,
                    Height = 1,
                    Opacity = 0.4
                });
            }
        }
    }
}
