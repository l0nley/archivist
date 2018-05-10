using Archivist.Core.Operations;
using Archivist.Core.Operations.Remote;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Archivist.Core
{
    class Program
    {
        private static Dictionary<string, Func<AbstractOperation>> Commands { get; set; }
        private static Dictionary<string, string> Help { get; set; }
        private static Util.Environment Env { get; set; }
        private static string ConnectionString { get; set; }
        private static string ContainerName { get; set; }
        static void Main(string[] args)
        {
            string configText;
            using(var file = File.OpenRead("config.json"))
            {
                using(TextReader tr = new StreamReader(file))
                {
                    configText = tr.ReadToEnd();
                }
            }
            var config = JsonConvert.DeserializeObject<Config>(configText);
            ConnectionString = config.ConnectionString;
            ContainerName = config.ContainerName;
            Console.InputEncoding = Encoding.Unicode;
            Console.OutputEncoding = Encoding.Unicode;
            Commands = new Dictionary<string, Func<AbstractOperation>>();
            Help = new Dictionary<string, string>();
            LoadCommands();
            Console.WriteLine($"{Commands.Count} operations found");
            Env = new Util.Environment
            {
                LocalPath = Environment.CurrentDirectory.TrimEnd(Path.DirectorySeparatorChar),
                RemotePath = string.Empty
            };
            while (true)
            {
                Console.Write($"{Env.LocalPath}@{Env.RemotePath}#>");
                var cmd = Console.ReadLine();
                if (cmd == "exit")
                {
                    return;
                }
                ProcessCommand(cmd);
            }
        }

        static void ProcessCommand(string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd))
            {
                return;
            }
            if (cmd == "help")
            {
                foreach (var item in Help)
                {
                    Console.WriteLine($"{item.Key} - {item.Value}");
                }
                return;
            }

            var splitted = cmd.Split(' ');
            var cmdName = splitted[0];
            var lines = splitted.Skip(1).ToArray();
            if (false == Commands.ContainsKey(cmdName))
            {
                Console.WriteLine($"Command {cmdName} not found.");
                return;
            }
            var instance = Commands[cmdName]();
            instance.ExecuteAsync(Env, lines).GetAwaiter().GetResult();
        }

        static void LoadCommands()
        {
            var commands = Assembly.GetExecutingAssembly().GetTypes()
                .Where(_ => _.IsSubclassOf(typeof(AbstractOperation)) && _.IsAbstract == false).ToArray();
            foreach (var command in commands)
            {
                Func<AbstractOperation> creator = null;
                if (command.IsSubclassOf(typeof(AbstractCloudOperation)))
                {
                    creator = () =>
                    {
                        return (AbstractCloudOperation)Activator.CreateInstance(command, ConnectionString, ContainerName, Guid.NewGuid());
                    };
                }
                else
                {
                    creator = () =>
                    {
                        return (AbstractOperation)Activator.CreateInstance(command, Guid.NewGuid());
                    };
                }
                var instance = creator();
                Help.Add(instance.Name, instance.Description);
                Commands.Add(instance.Name, creator);
            }
        }
    }
}
