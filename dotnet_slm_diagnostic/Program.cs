using DotNetSlmEfficiency;

Console.WriteLine("=== .NET SLM CPU Efficiency Comparison ===\n");
Console.WriteLine("Choose test to run:");
Console.WriteLine("1. Foundry Local (CPU)");
Console.WriteLine("2. Ollama (CPU)");
Console.WriteLine("3. Run Both");
Console.WriteLine("4. Test Enhanced Monitoring");
Console.Write("\nEnter choice (1-4): ");

var choice = Console.ReadLine();

switch (choice)
{
    case "1":
        try
        {
            await RunFoundryLocalTest();
        }
        catch (Exception ex)
        {
            // Check if this is a connection issue with Foundry Local
            string exceptionInfo = ex.ToString().ToLower();
            if (exceptionInfo.Contains("foundry local") || exceptionInfo.Contains("actively refused") || 
                exceptionInfo.Contains("no connection could be made") || exceptionInfo.Contains("connection refused"))
            {
                Console.WriteLine("\n" + new string('=', 60));
                Console.WriteLine("FOUNDRY LOCAL NOT AVAILABLE");
                Console.WriteLine(new string('=', 60));
                Console.WriteLine("Foundry Local is not running or not accessible.");
                Console.WriteLine("To run this test, please:");
                Console.WriteLine("1. Install Foundry Local if not already installed");
                Console.WriteLine("2. Run: foundry model run qwen2.5-0.5b-instruct-generic-cpu:4");
                Console.WriteLine("3. Ensure the service is running on port 55588");
                Console.WriteLine("4. Try the test again");
                Console.WriteLine(new string('=', 60));
            }
            else
            {
                // Re-throw if it's not a connection issue
                throw;
            }
        }
        break;
    case "2":
        await RunOllamaTest();
        break;
    case "3":
        try
        {
            await RunFoundryLocalTest();
            
            // Wait for process to fully complete before starting next test
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("Waiting 3 seconds before starting next test...");
            Console.WriteLine(new string('=', 60) + "\n");
            await Task.Delay(3000);
        }
        catch (Exception ex)
        {
            // Check if this is a connection issue with Foundry Local
            string exceptionInfo = ex.ToString().ToLower();
            if (exceptionInfo.Contains("foundry local") || exceptionInfo.Contains("actively refused") || 
                exceptionInfo.Contains("no connection could be made") || exceptionInfo.Contains("connection refused"))
            {
                Console.WriteLine("\n" + new string('=', 60));
                Console.WriteLine("SKIPPING FOUNDRY LOCAL - NOT AVAILABLE");
                Console.WriteLine(new string('=', 60));
                Console.WriteLine("Foundry Local is not running. Proceeding with Ollama test only.");
                Console.WriteLine(new string('=', 60) + "\n");
            }
            else
            {
                // Re-throw if it's not a connection issue
                throw;
            }
        }
        
        await RunOllamaTest();
        break;
    case "4":
        await TestEnhancedMonitoring.RunTest();
        break;
    default:
        Console.WriteLine("Invalid choice. Exiting.");
        break;
}

Console.WriteLine("\n" + new string('=', 60));
Console.WriteLine("All tests completed. Press any key to exit.");
Console.WriteLine(new string('=', 60));
try
{
    Console.ReadKey();
}
catch (InvalidOperationException)
{
    // Handle case when console input is redirected or not available
    Console.WriteLine("Console input not available, exiting gracefully.");
}

async Task RunFoundryLocalTest()
{
    Console.WriteLine("\n=== Testing Foundry Local ===");
    using var client = new FoundryLocalClient();
    await client.RunInferenceWithMonitoring();
}

async Task RunOllamaTest()
{
    Console.WriteLine("\n=== Testing Ollama ===");
    using var client = new OllamaClient();
    await client.RunInferenceWithMonitoring();
}
