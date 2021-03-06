﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using KoalaBot;
using KoalaBot.Database;
using System.Diagnostics;

namespace KoalaBot
{
    class Program
    {

        private static string configFile = "config.json";
        private static string logFile = "koala.log";

        static void Main(string[] args)
        {
            //
            //
            //if (success)
            //{
            //}

            using (var tokenSource = new CancellationTokenSource())
            {
                Console.WriteLine("Starting Bot...");
                var task = MainAsync(args, tokenSource.Token);
                var readline = true;

                Console.WriteLine("Input Active");
                do
                {
                    string line = Console.ReadLine();
                    string[] prts = line.Split(' ');
                    for (int i = 0; i < prts.Length; i++)
                    {
                        switch (prts[0])
                        {
                            default:
                                Console.WriteLine("Unkown Command " + prts[0]);
                                break;
                                

                            case "quit":
                            case "exit":
                            case "close":
                            case "stop":
                                Console.WriteLine("Stopping Bot...");
                                tokenSource.Cancel();
                                task.Wait();
                                break;
                        }
                    }
                } while (readline && !task.IsCanceled);

                Console.WriteLine("Bot Terminated...");
            }
        }

        static async Task MainAsync(string[] args, CancellationToken cancellationToken)
        {
            //prepare the config
            bool appendLog = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-config":
                        configFile = args[++i];
                        break;

                    case "-log":
                        logFile = args[++i];
                        break;

                    case "-appendLog":
                        appendLog = true;
                        break;
                }
            }

            //Prepare the logging
            KoalaBot.Logging.OutputLogQueue.Initialize(logFile, appendLog);

            //Load the config
            BotConfig config = new BotConfig();
            if (File.Exists(configFile))
            {
                Console.WriteLine("Loading Configuration: {0}", configFile);
                string json = await File.ReadAllTextAsync(configFile);
                try
                {
                    config = JsonConvert.DeserializeObject<BotConfig>(json);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }
            }
            else
            {
                //Save the config 
                Console.WriteLine("Aborting because first time generating the configuration file.");
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                await File.WriteAllTextAsync(configFile, json);

                //Create the token file too if it doesnt exist.
                if (!File.Exists(config.TokenFile))
                    await File.WriteAllTextAsync(config.TokenFile, "<BOT TOKEN HERE>");
                return;
            }

            //var db = new DatabaseClient(config.SQL.Address, config.SQL.Database, config.SQL.Username, config.SQL.Password, "k_", 3306);
            //var success = db.OpenAsync().Result;

            //Create the instance
            Console.WriteLine("Creating Bot...");
            Koala bot = new Koala(config);
            
            Console.WriteLine("Initializing Bot...");
            await bot.InitAsync();

            Console.WriteLine("Done.");
            await Task.Delay(-1, cancellationToken);

            Console.WriteLine("Deinitializing Bot...");
            await bot.DeinitAsync();
        }
    }
}
