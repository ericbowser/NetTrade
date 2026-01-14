using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetTrade.Client;
using NetTrade.Service;
using IAlpacaCryptoDataClient = NetTrade.Service.IAlpacaCryptoDataClient;
using Microsoft.OpenApi.Models;

namespace NetTrade
{
    public sealed class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowLocalhost",
                    x => x
                    .WithOrigins("https://localhost:3002", "http://localhost:3002")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
            });
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "CoinAPI - Trading & Backtest API",
                    Version = "v1",
                    Description = "API for running trading strategies, backtests, and managing Alpaca Paper Trading accounts",
                    Contact = new OpenApiContact
                    {
                        Name = "CoinAPI Support"
                    }
                });
                
                // Use full type name to avoid schema ID conflicts between different libraries
                // This ensures Alpaca.Markets.OrderSide and Coinbase.AdvancedTrade.Enums.OrderSide
                // get different schema IDs
                c.CustomSchemaIds(type =>
                {
                    if (type.FullName == null)
                        return type.Name;
                    
                    // Replace characters that aren't valid in schema IDs
                    return type.FullName
                        .Replace("+", "_")  // Nested types
                        .Replace("`", "_")  // Generic type markers
                        .Replace("[", "_")
                        .Replace("]", "_");
                });
                
                // Include XML comments if available
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });
            // Load appsettings.json - try multiple paths
            var appsettingsPath = "appsettings.json";
            if (!File.Exists(appsettingsPath))
            {
                appsettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            }
            if (File.Exists(appsettingsPath))
            {
                builder.Configuration.AddJsonFile(appsettingsPath, optional: false, reloadOnChange: true);
            }
            else
            {
                // Try CoinAPI directory
                appsettingsPath = Path.Combine("CoinAPI", "appsettings.json");
                if (File.Exists(appsettingsPath))
                {
                    builder.Configuration.AddJsonFile(appsettingsPath, optional: false, reloadOnChange: true);
                }
            }

            // Config
            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

            // Client
            builder.Services.AddHttpClient<IAlpacaGridTradingService, AlpacaGridTradingService>();
            builder.Services.AddSingleton<IAlpacaPaperClient, AlpacaPaperClient>();
            builder.Services.AddTransient<IAlpacaCryptoDataClient, AlpacaCryptoDataService>();

            // Services
            builder.Services.AddTransient<IScalpingStrategyService, ScalpingStrategyService>();
            builder.Services.AddTransient<IGridBacktestService, GridStrategyService>();
            builder.Services.AddTransient<IRSIStrategyService, RSIStrategyService>();
            builder.Services.AddTransient<IBollingerBandsStrategyService, BollingerBandsStrategyService>();
            builder.Services.AddTransient<IMovingAverageCrossoverStrategyService, MovingAverageCrossoverStrategyService>();
            
            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "NetTrade V1");
                c.RoutePrefix = "swagger";
                c.DisplayRequestDuration();
                c.EnableTryItOutByDefault();
            });

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.UseCors("AllowLocalhost");
            app.MapControllers();
            TimeZoneInfo.ClearCachedData();

            await app.RunAsync();
        }
    }
}