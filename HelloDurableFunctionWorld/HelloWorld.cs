using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace HelloDurableFunctionWorld
{
    public static class HelloWorld
    {
        [FunctionName("A_HelloWorld_Durable_Function")]
        public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var who = await context.WaitForExternalEvent<string>("Who-is-calling");

            return who;
        }

        [FunctionName("Start_A_HelloWorld_DurableFunction")]
        public static async Task<HttpResponseMessage> HttpStart([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage req, [DurableClient] IDurableOrchestrationClient starter, ILogger log)
        {
            var instanceId = await starter.StartNewAsync("A_HelloWorld_Durable_Function", null);

            log.LogInformation($"Started orchestration with ID={instanceId}");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}