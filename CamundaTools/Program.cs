using CamundaTools;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
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
            Console.WriteLine("Please select an option:");
            Console.WriteLine("1. Retry All Process (activity based)");
            Console.WriteLine("1. Retry All External Process (activity based)");
            Console.WriteLine("3. Exit");
            string choice = "12387281942";
            while(choice != "3")
            {
                Console.Write("Input >> ");
                choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        RetryAllProcess().Wait();
                        break;
                    case "2":
                        RetryAllProcess().Wait();
                        break;
                    case "3":
                        Console.WriteLine("Exiting program...");
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please select a valid option.");
                        break;
                }
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
                Console.WriteLine("Data available " + data.Count());

                int dataSize = data.Count();
                if (dataSize == 0)
                {
                    Console.WriteLine("No data process in this workflow");
                    Console.ReadKey();
                    return;
                }
                bool isFinished = false;
                HttpResponseMessage jobResponse = new HttpResponseMessage();
                Console.WriteLine("Processing...");
                for (int i = 0; i < dataSize; i++)
                {
                    string jobProcessInstance = baseUrl + CommonConstant.CAMUNDA_PROCESS_GET_PROCESS_INSTANCE_ENDPOINT;
                    jobProcessInstance += data[i].GetProperty("id").GetString();
                    jobResponse = await httpClient.GetAsync(jobProcessInstance);
                    if (!jobResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Failed to retrieve process instance. Status code: {response.StatusCode}");
                        return;
                    }
                    string rawResponseJob = await jobResponse.Content.ReadAsStringAsync();
                    var responseJob = JsonSerializer.Deserialize<List<JsonElement>>(rawResponseJob);
                    string retryUrl = baseUrl + CommonConstant.CAMUNDA_PROCESS_RETRY_ENDPOINT + responseJob[0].GetProperty("id").GetString() + "/retries";
                    var retryBody = new { retries = 1 };
                    string retryJson = JsonSerializer.Serialize(retryBody);
                    StringContent retryContent = new StringContent(retryJson, Encoding.UTF8, "application/json");
                    HttpResponseMessage retryResponse = await httpClient.PutAsync(retryUrl, retryContent);

                    if (i == dataSize - 1)
                    {
                        isFinished = true;
                        Console.WriteLine("Process Complete!");
                        Console.ReadKey();
                    }

                }
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


