using CamundaTools;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CamundaTools
{
    public class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();
        static async Task Main(string[] args)
        {
            Programs();
        }

        static void Programs()
        {
            Console.WriteLine("Welcome to Camunda Tools!!");
            Console.WriteLine("Developed by: Valeska Valentin Ekklesia");
            string choice;
            Console.WriteLine("Please select an option:");
            Console.WriteLine("1. Retry All Process (activity based)");
            choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    RetryAllProcess().Wait();
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please select a valid option.");
                    break;
            }
        }

        async static Task RetryAllProcess()
        {
            Console.Write("Please input your base Camunda URL: ");
            string baseUrl = Console.ReadLine();
            Console.Write("Please input your Camunda Process Definition ID (ex: WF_PAY_RCV_PROCESS_AR:1:c33d9a4f-ef7d-11f0-adc8-3a8ae8da5715): ");
            string processDefinitionId = Console.ReadLine();
            string processDefinitionUrl = baseUrl + CommonConstant.CAMUNDA_PROCESS_DEFINITION_ENDPOINT + processDefinitionId;
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(processDefinitionUrl);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to retrieve process definition. Status code: {response.StatusCode}");
                    return;
                }
                string responseBody = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<List<JsonElement>>(responseBody);
                Console.WriteLine("Data available " + data.Count);
                //Console.WriteLine(responseBody);
                Console.ReadKey();
            }
            catch (Exception ex) {
                Console.WriteLine($"Error: {ex.Message}");
                Console.ReadKey();
            }

        }
    }
}


