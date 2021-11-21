using GenshinAcademyBridge.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using GenshinAcademyBridge.Configuration;
using GenshinAcademyBridge.Modules;

namespace GenshinAcademyBridge
{
    class Program
    {
        public const string ConfigPath = "configuration/";
        public const string BridgesPath = ConfigPath + "bridges/";
        public static TgBot TgBot;
        public static VkBot VkBot;
        public static List<Bridge> Bridges;

        public static Dictionary<long, long> MessagesIds;


        public static ServiceCollection Services { get; private set; }

        static void Main()
        {
            Services = new ServiceCollection();
            SetupBridges();
            SetupLogger();
            MessagesIds = new Dictionary<long, long>();
            TgBot = new TgBot();

            VkBot = new VkBot();

            while (true)
            {

            }
        }

        private static void SetupBridges()
        {
            if (!Directory.Exists(BridgesPath)) Directory.CreateDirectory(BridgesPath);

            if (Directory.GetFiles(BridgesPath).Length == 0)
            {
                var cfg = new Bridge();
                Console.WriteLine("Set bridge title:");
                var title = Console.ReadLine();
                Console.WriteLine("Set VK conversation id:");
                cfg.VkId = long.Parse(Console.ReadLine() ?? string.Empty);
                Console.WriteLine("Set TG conversation id:");
                cfg.TgId = long.Parse(Console.ReadLine() ?? string.Empty);
                JsonStorage.StoreObject(cfg, $"{BridgesPath}/{title}.json");
            }
            Bridges = new List<Bridge>();
            foreach (var bridge in Directory.GetFiles(BridgesPath))
            {
                Bridges.Add(JsonStorage.RestoreObject<Bridge>(bridge));
            }
        }

        private static void SetupLogger()
        {
            Log.Logger = new LoggerConfiguration()
        .MinimumLevel
        .Information()
        .WriteTo
        .Console()
        .WriteTo
        .File("log.txt",
            rollingInterval: RollingInterval.Day,
            rollOnFileSizeLimit: true)
        .CreateLogger();

            Services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddSerilog(dispose: true);
            });
        }
    }
}
