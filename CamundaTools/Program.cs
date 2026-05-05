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
            Console.WriteLine("Developed by: BoboMad");
            Console.WriteLine("Please select an option:");
            Console.WriteLine("1. Retry All Incident Process (activity based)");
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
            Console.Write("Do you want to loop? [y/n] : ");
            string isLoop = Console.ReadLine().ToLower();

            string incidentUrl = baseUrl + "/engine-rest/incident?processDefinitionId=" + processDefinitionId;

            do
            {
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(incidentUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Failed to retrieve incidents. Status code: {response.StatusCode}");
                        Console.ReadKey();
                        return;
                    }

                    string responseBody = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<List<JsonElement>>(responseBody);
                    int dataSize = data.Count();
                    Console.WriteLine($"Incident data available: {dataSize}");

                    if (dataSize == 0)
                    {
                        Console.WriteLine("No incidents found for this process definition.");
                        if(isLoop == "y")
                        {
                            Console.WriteLine("Cooldown 10 sec...");
                            await Task.Delay(10000);
                            continue;
                        }
                        Console.ReadKey();
                        return;
                    }
                    Console.WriteLine("Processing...");

                    int successCount = 0;
                    int failCount = 0;

                    for (int i = 0; i < dataSize; i++)
                    {
                        JsonElement incident = data[i];

                        string incidentType = incident.GetProperty("incidentType").GetString();

                        string jobId = null;
                        if (incidentType == "failedJob")
                        {
                            if (!incident.TryGetProperty("configuration", out JsonElement configElement)
                                || configElement.ValueKind == JsonValueKind.Null)
                            {
                                Console.WriteLine($"[{i + 1}/{dataSize}] Skipped — no configuration (jobId) found.");
                                continue;
                            }
                            jobId = configElement.GetString();
                        }
                        else
                        {
                            Console.WriteLine($"[{i + 1}/{dataSize}] Skipped — unsupported incident type: {incidentType}");
                            continue;
                        }

                        Console.WriteLine($"[{i + 1}/{dataSize}] Retrying job: {jobId}");

                        string retryUrl = baseUrl + "/engine-rest/job/" + jobId + "/retries";
                        var retryBody = new { retries = 1 };
                        string retryJson = JsonSerializer.Serialize(retryBody);
                        StringContent retryContent = new StringContent(retryJson, Encoding.UTF8, "application/json");

                        HttpResponseMessage retryResponse = await httpClient.PutAsync(retryUrl, retryContent);

                        if (retryResponse.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"[{i + 1}/{dataSize}] Retried successfully: {jobId}");
                            successCount++;
                        }
                        else
                        {
                            string errorBody = await retryResponse.Content.ReadAsStringAsync();
                            Console.WriteLine($"[{i + 1}/{dataSize}] Retry failed. Status: {retryResponse.StatusCode} | {errorBody}");
                            failCount++;
                        }

                        Console.WriteLine("Waiting 20 sec...");
                        await Task.Delay(20000);
                        Console.WriteLine($"[{i + 1}/{dataSize}] Done.\n");
                    }

                    Console.WriteLine($"\nProcess Complete! Success: {successCount} | Failed: {failCount}");
                    if(isLoop == "y")
                    {
                        Console.WriteLine("Cooldown 30 sec...");
                        await Task.Delay(30000);
                    }
                    else
                    {
                        Console.WriteLine("Press any key to continue");
                        Console.ReadKey();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.ReadKey();
                }
            } while (isLoop == "y");
        }
    }
}


