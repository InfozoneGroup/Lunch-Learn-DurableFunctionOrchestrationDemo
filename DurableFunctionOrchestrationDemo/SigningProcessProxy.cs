using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using SharedLib;

namespace DurableFunctionOrchestrationDemo
{
    public static class SigningProcessProxy
    {
        [FunctionName("Status")]
        public static async Task<IActionResult> HttpSigningProcessStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/status/{id}")] HttpRequestMessage req, 
            [DurableClient] IDurableOrchestrationClient client, 
            ILogger log,
            string id) => new OkObjectResult(client.GetStatusAsync(id, true, true, true).GetAwaiter().GetResult());

        [FunctionName("Start")]
        public static async Task<IActionResult> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/start/{numberOfSignees}")] HttpRequestMessage req, 
            [DurableClient] IDurableOrchestrationClient starter, 
            ILogger log,
            int numberOfSignees)
        {
            var instanceIds = Enumerable.Range(0, numberOfSignees).Select(x => Guid.NewGuid().ToString());
            var signees = instanceIds.Select(x => new Signee {Id = x, Email = $"{x.Substring(0, 6)}@test.com"}).ToArray();

            var instanceId = await starter.StartNewAsync("SigningProcess", null, signees);

            log.LogInformation($"Started SigningProcess orchestration with ID = '{instanceId}'.");

            var response = starter.CreateCheckStatusResponse(req, instanceId);
            var body = await response.Content.ReadAsStringAsync();
            log.LogInformation($"CheckStatusRespone:{Environment.NewLine}{body}");

            return new OkObjectResult(new SigningProcess {InstanceId = instanceId, Signees = signees});
        }

        [FunctionName("Callback")]
        public static async Task<IActionResult> Callback(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/callback/{id}/{status}")] HttpRequestMessage req, 
            [DurableClient] IDurableOrchestrationClient client, 
            ILogger log,
            string id,
            string status)
        {
            var instanceStatus = await client.GetStatusAsync(id);
            try
            {
                await client.RaiseEventAsync(id, "signeeevent", status);
            }
            catch (Exception e)
            {
                log.LogWarning(e, $"Raise event failed, status for instance: {instanceStatus.RuntimeStatus}");
                
                return new NoContentResult();
            }

            return new AcceptedResult();
        }
    }
}