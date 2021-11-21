using GenshinAcademyBridge.Configuration;
using GenshinAcademyBridge.Extensions;
using GenshinAcademyBridge.Modules;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GenshinAcademyBridge
{
    public class ChatBridgeService : IHostedService
    {
        public const string ConfigPath = "configuration/";
        public const string BridgesPath = ConfigPath + "bridges/";

        public ChatBridgeService(
            VkBot vk,
            TgBot tg)
        {
            SetupBridges();
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
            Program.Bridges = new List<Bridge>();
            foreach (var bridge in Directory.GetFiles(BridgesPath))
            {
                Program.Bridges.Add(JsonStorage.RestoreObject<Bridge>(bridge));
            }
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                await Task.Delay(5000);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
