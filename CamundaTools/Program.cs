using CamundaTools;
using System.Diagnostics;
using System.Linq;
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
            Console.WriteLine("2. Retry All Incident External Process (activity based)");
            Console.WriteLine("3. Complete user task");
            Console.WriteLine("4. Migrate Instance");
            Console.WriteLine("5. Exit");
            string choice = "12387281942";
            while(choice != "5")
            {
                Console.Write("Input >> ");
                choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        RetryAllIncidentProcess().Wait();
                        break;
                    case "2":
                        RetryAllIncidentExternalProcess().Wait();
                        break;
                    case "3":
                        CompleteUserTask().Wait();
                        break;
                    case "4":
                        MigrateInstance().Wait();
                        break;
                    case "5":
                        Console.WriteLine("Exiting program...");
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please select a valid option.");
                        break;
                }
            }
        }

        async static Task RetryAllIncidentProcess()
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
                            Console.WriteLine("Cooldown 30 sec...");
                            await Task.Delay(30000);
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

        async static Task RetryAllIncidentExternalProcess()
        {
            Console.Write("Please input your base Camunda URL: ");
            string baseUrl = Console.ReadLine();
            Console.Write("Please input your Camunda Process Definition ID (ex: WF_PAY_RCV_PROCESS_AR:1:c33d9a4f-ef7d-11f0-adc8-3a8ae8da5715): ");
            string processDefinitionId = Console.ReadLine();

            string listExternalTaskProcessUrl = baseUrl + "/engine-rest/incident?processDefinitionId=" + processDefinitionId;

            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(listExternalTaskProcessUrl);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to retrieve external tasks. Status code: {response.StatusCode}");
                    Console.ReadKey();
                    return;
                }

                string responseBody = await response.Content.ReadAsStringAsync();
                var listExternalTaskProcess = JsonSerializer.Deserialize<List<JsonElement>>(responseBody);
                int dataSize = listExternalTaskProcess.Count();

                if(dataSize == 0)
                {
                    Console.WriteLine("Data is empty!");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine("Processing...");

                int successCount = 0;
                int failCount = 0;

                for(int i = 0; i < dataSize; i++)
                {
                    JsonElement incident = listExternalTaskProcess[i];

                    string incidentType = incident.GetProperty("incidentType").GetString();

                    string externalTaskId = null;
                    if (incidentType == "failedExternalTask")
                    {
                        if (!incident.TryGetProperty("configuration", out JsonElement configElement)
                            || configElement.ValueKind == JsonValueKind.Null)
                        {
                            Console.WriteLine($"[{i + 1}/{dataSize}] Skipped — no configuration (jobId) found.");
                            continue;
                        }
                        externalTaskId = configElement.GetString();
                    }
                    else
                    {
                        Console.WriteLine($"[{i + 1}/{dataSize}] Skipped — unsupported incident type: {incidentType}");
                        continue;
                    }

                    Console.WriteLine($"[{i + 1}/{dataSize}] Retrying job: {externalTaskId}");

                    string retryUrl = baseUrl + "/engine-rest/external-task/retries-async";
                    var retryBody = new { retries = 1, externalTaskIds = new[] { externalTaskId } };
                    string retryJson = JsonSerializer.Serialize(retryBody);
                    StringContent retryContent = new StringContent(retryJson, Encoding.UTF8, "application/json");

                    HttpResponseMessage retryResponse = await httpClient.PostAsync(retryUrl, retryContent);

                    if (retryResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[{i + 1}/{dataSize}] Retried successfully: {externalTaskId}");
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.ReadKey();
            }
        }

        async static Task CompleteUserTask()
        {
            Console.Write("Please input your base Camunda URL: ");
            string baseUrl = Console.ReadLine()?.TrimEnd('/');

            Console.Write("Drop Process Definition Id here: ");
            string processDefinitionId = Console.ReadLine();

            Console.Write("Provide Instance Id that need to be completed: ");
            string processInstanceId = Console.ReadLine();

            Console.WriteLine("\nFetching tasks...");
            string listTaskUrl = $"{baseUrl}/engine-rest/task?processDefinitionId={processDefinitionId}";
            HttpResponseMessage responseTasks = await httpClient.GetAsync(listTaskUrl);

            if (!responseTasks.IsSuccessStatusCode)
            {
                string errBody = await responseTasks.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to retrieve tasks. Status: {responseTasks.StatusCode} | {errBody}");
                Console.ReadKey();
                return;
            }

            string responseBody = await responseTasks.Content.ReadAsStringAsync();
            var listTasks = JsonSerializer.Deserialize<List<JsonElement>>(responseBody);

            if (listTasks == null || listTasks.Count == 0)
            {
                Console.WriteLine("No active tasks found for the given Process Definition ID.");
                Console.ReadKey();
                return;
            }
            var matchedTasks = listTasks
                .Where(task => task.GetProperty("processInstanceId").GetString() == processInstanceId)
                .ToList();

            if (matchedTasks.Count == 0)
            {
                Console.WriteLine($"No tasks found for Process Instance ID: {processInstanceId}");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"Found {matchedTasks.Count} task(s) for instance {processInstanceId}. Completing...\n");

            int successCount = 0;
            int failCount = 0;

            foreach (var task in matchedTasks)
            {
                string taskId = task.GetProperty("id").GetString();
                string taskName = task.GetProperty("name").GetString();

                string completeTaskUrl = $"{baseUrl}/engine-rest/task/{taskId}/complete";
                string bodyJson = JsonSerializer.Serialize(new { });
                StringContent bodyContent = new StringContent(bodyJson, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.PostAsync(completeTaskUrl, bodyContent);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[SUCCESS] Task '{taskName}' (ID: {taskId}) completed.");
                    successCount++;
                }
                else
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[FAILED]  Task '{taskName}' (ID: {taskId}) | Status: {response.StatusCode} | {errorBody}");
                    failCount++;
                }
            }

            Console.WriteLine($"\n--- Process Complete ---");
            Console.WriteLine($"Success : {successCount}");
            Console.WriteLine($"Failed  : {failCount}");
            Console.WriteLine($"Total   : {matchedTasks.Count}");
            Console.ReadKey();
        }

        async static Task MigrateInstance()
        {
            Console.Write("Please input your base Camunda URL: ");
            string baseUrl = Console.ReadLine();
            Console.Write("Please input your old Process Definition ID (ex: WF_PAY_RCV_PROCESS_AR:1:c33d9a4f-ef7d-11f0-adc8-3a8ae8da5715): ");
            string oldProcessDefinitionId = Console.ReadLine();
            Console.Write("Please input your new Process Definition ID (ex: WF_PAY_RCV_PROCESS_AR:2:c33d9a4f-ef7d-11f0-adc8-3a8ae8da5715): ");
            string newProcessDefinitionId = Console.ReadLine();
            string migrationChoice = "";

            while (migrationChoice != "1" && migrationChoice != "2")
            {
                Console.WriteLine("Choose way to migrate:");
                Console.WriteLine("1. Auto (use the same activity id)");
                Console.WriteLine("2. Manual (decide activity id on your own)");
                Console.Write("Choice >> ");
                migrationChoice = Console.ReadLine();
                if(migrationChoice != "1" &&  migrationChoice != "2")
                {
                    Console.WriteLine("Wrong input, please choose again!\n");
                }
            }

            if(migrationChoice == "1")
            {

            }
            else
            {

            }
        }
    }
}


