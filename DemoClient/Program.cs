using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using SharedLib;

namespace DemoClient
{
    internal static class Program
    {
        private const string StartUrl = "http://localhost:7071/api/api/start/{0}";
        private const string StatusUrl = "http://localhost:7071/api/api/status/{0}";
        private const string CallbackUrl = "http://localhost:7071/api/api/callback/{0}/{1}";
        
        private static readonly HttpClient HttpClient = new();

        private static void Main()
        {
            Console.WriteLine("Welcome");

            while (true)
            {
                Console.WriteLine();
                WriteMainMenu();
                Console.Write("Command: ");
                
                var cmd = Console.ReadLine() ?? "";
                if (cmd.ToLower() == "q")
                {
                    return;
                }

                if (cmd.StartsWith("n"))
                {
                    int signeeCount = int.TryParse($"{cmd}  ".Substring(2), out signeeCount) ? signeeCount : 2;
                    var startResult = HttpGet<SigningProcess>(string.Format(StartUrl, signeeCount));
                    
                    while (true)
                    {
                        Console.WriteLine("-------------------");
                        Console.WriteLine($"Signing process '{startResult.InstanceId}'");

                        WriteSubMenu(startResult);

                        Console.Write("Command: ");
                        cmd = Console.ReadLine()?.ToLower() ?? "";
                        Console.WriteLine();

                        if (cmd == "b")
                        {
                            break;
                        }

                        if (cmd == "s")
                        {
                            var statusJson = HttpGet(string.Format(StatusUrl, startResult.InstanceId));

                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine(statusJson);
                            Console.ResetColor();
                        }
                        else
                        {

                            if (cmd.StartsWith("ok.") || cmd.StartsWith("reject.") || cmd.StartsWith("s."))
                            {
                                var parts = cmd.Split('.');
                                if (parts.Length == 2)
                                {
                                    var instanceId = parts.Last();
                                    var instanceCmd = parts.First().ToUpper();
                                    var requestUri = instanceCmd == "S" ? string.Format(StatusUrl, instanceId) : string.Format(CallbackUrl, instanceId, instanceCmd);
                                    
                                    Console.ForegroundColor = ConsoleColor.Magenta;
                                    Console.WriteLine(HttpGet(requestUri));
                                    Console.ResetColor();
                                }
                            }
                        }
                    }
                }
            }
        }

        private static string HttpGet(string requestUri)
        {
            var httpResponseMessage = HttpClient.GetAsync(requestUri).GetAwaiter().GetResult();
            var body = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            return string.IsNullOrWhiteSpace(body) ? httpResponseMessage.StatusCode.ToString() : body;
        }

        private static T HttpGet<T>(string requestUri) => JsonSerializer.Deserialize<T>(HttpGet(requestUri), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        private static void WriteSubMenu(SigningProcess signingProcess)
        {
            Console.WriteLine();
            Console.WriteLine("S: status for process");
            Console.WriteLine();
            
            signingProcess.Signees.ToList().ForEach(x =>
            {
                Console.WriteLine($"OK.{x.Id}     : OK for signee {x.Id}");
                Console.WriteLine($"REJECT.{x.Id} : reject for signee {x.Id}");
                Console.WriteLine($"S.{x.Id}      : status for signee {x.Id}");
                Console.WriteLine();
            });
            

            Console.WriteLine("B: back");
        }

        private static void WriteMainMenu()
        {
            Console.WriteLine("N   : start new acc. process (default is 2 signees)");
            Console.WriteLine("N.X : start new acc. process with X signees");
            Console.WriteLine("Q   : exit");
        }
    }
}
