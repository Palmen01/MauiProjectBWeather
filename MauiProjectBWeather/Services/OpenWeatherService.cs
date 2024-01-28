using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json; 
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;

using MauiProjectBWeather.Models;
using System.Collections.Concurrent;

namespace MauiProjectBWeather.Services
{
    public class OpenWeatherService
    {
        HttpClient httpClient = new HttpClient();

        //Cache declaration
        ConcurrentDictionary<(double, double, string), Forecast> cachedGeoForecasts = new ConcurrentDictionary<(double, double, string), Forecast>();
        ConcurrentDictionary<(string, string), Forecast> cachedCityForecasts = new ConcurrentDictionary<(string, string), Forecast>();

        //Your API Key
        readonly string apiKey = "7caac1dfce0b860bc1dd245884f44b94";

        //Event declaration
        public event EventHandler<string> WeatherForecastAvailable;
        public event EventHandler<string> CachedForecastAvailable;


        protected virtual void OnCachedForecastAvailable(string message)
        {
            CachedForecastAvailable?.Invoke(this, message);
        }
        protected virtual void OnWeatherForecastAvailable(string message)
        {
            WeatherForecastAvailable?.Invoke(this, message);
        }

        public async Task<Forecast> GetForecastAsync(string City)
        {
            //part of cache code here to check if forecast in Cache
            //generate an event that shows forecast was from cache
            //Your code
            if (cachedCityForecasts.ContainsKey((City, DateTime.Now.ToString("yyyy-MM-dd HH:mm"))))
            {
                OnCachedForecastAvailable(City);
                cachedCityForecasts.TryGetValue((City, DateTime.Now.ToString("yyyy-MM-dd HH:mm")), out Forecast cashedForecast);
                return cashedForecast;
            }

            //https://openweathermap.org/current
            var language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var uri = $"https://api.openweathermap.org/data/2.5/forecast?q={City}&units=metric&lang={language}&appid={apiKey}";

            Forecast forecast = await ReadWebApiAsync(uri);

            //part of event and cache code here
            //generate an event with different message if cached data
            //Your code
            cachedCityForecasts.TryAdd((City, DateTime.Now.ToString("yyyy-MM-dd HH:mm")), forecast);
            OnWeatherForecastAvailable(City);
            return forecast;

        }
        public async Task<Forecast> GetForecastAsync(double latitude, double longitude)
        {
            //part of cache code here to check if forecast in Cache
            //generate an event that shows forecast was from cache
            //Your code
            if (cachedGeoForecasts.ContainsKey((latitude, longitude, DateTime.Now.ToString("yyyy-MM-dd HH:mm"))))
            {
                OnCachedForecastAvailable($"{latitude}, {longitude}");
                cachedGeoForecasts.TryGetValue((latitude, longitude, DateTime.Now.ToString("yyyy-MM-dd HH:mm")), out Forecast cashedForecast);
                return cashedForecast;
            }

            //https://openweathermap.org/current
            var language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var uri = $"https://api.openweathermap.org/data/2.5/forecast?lat={latitude}&lon={longitude}&units=metric&lang={language}&appid={apiKey}";

            Forecast forecast = await ReadWebApiAsync(uri);

            //part of event and cache code here
            //generate an event with different message if cached data
            //Your code
            cachedGeoForecasts.TryAdd((latitude, longitude, DateTime.Now.ToString("yyyy-MM-dd HH:mm")), forecast);
            OnWeatherForecastAvailable($"{latitude}, {longitude}");
            return forecast;
        }
        private async Task<Forecast> ReadWebApiAsync(string uri)
        {
            //Read the response from the WebApi
            HttpResponseMessage response = await httpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            WeatherApiData wd = await response.Content.ReadFromJsonAsync<WeatherApiData>();


            //Convert WeatherApiData to Forecast using Linq.
            //Your code
            var forecast = new Forecast()
            {
                City = wd.city.name,
                Items = wd.list.Select(item => new ForecastItem
                {
                    Temperature = item.main.temp,
                    WindSpeed = item.wind.speed,
                    Description = item.weather[0].description,
                    DateTime = UnixTimeStampToDateTime(item.dt)

                }).ToList()
            };
            return forecast;


        }
        private DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
    }
}
