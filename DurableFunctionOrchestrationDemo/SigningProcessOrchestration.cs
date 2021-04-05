using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using SharedLib;

namespace DurableFunctionOrchestrationDemo
{
    public static class SigningProcessOrchestration
    {
        [FunctionName("SigningProcess")]
        public static async Task<Signee[]> RunSigningProcess([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            log.LogInformation("---Entering SigningProcess---");

            var signees = context.GetInput<Signee[]>();

            // Start generating docs
            var docTasks = signees.Select(x => context.CallActivityAsync<(string signeeId, string documentName)>("GenerateDocumentForSignee", x)).ToList();

            await Task.WhenAll(docTasks);

            docTasks.ForEach(docTask => signees.First(x => x.Id == docTask.Result.signeeId).Document = docTask.Result.documentName);
            
            // Start sub orchs, eg. notify signee and wait for feedback/callback
            var callbackTasks = signees.Select(x => context.CallSubOrchestratorAsync<Signee>("SigneeCallback", x.Id, x)).ToList();
            
            await Task.WhenAll(callbackTasks);

            var result = callbackTasks.Select(x => x.Result).ToArray();

            return result;
        }

        [FunctionName("SigneeCallback")]
        public static async Task<Signee> RunSigneeCallbackOrchestration([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            log.LogInformation("---Entering SigneeCallback---");

            var signee = context.GetInput<Signee>();

            // Notify signee
            signee = await context.CallActivityAsync<Signee>("NotifiySignee", signee);

            context.SetCustomStatus("Signee notified");

            // Wait for feedback/callback
            var callbackStatus = await context.WaitForExternalEvent<string>("signeeevent");

            context.SetCustomStatus($"Signee called back {callbackStatus}");

            return signee;
        }

        [FunctionName("GenerateDocumentForSignee")]
        public static async Task<(string signeeId, string documentName)> RunGenerateDocumentForSigee([ActivityTrigger] Signee signee, ILogger log)
        {
            log.LogInformation($"---DUMMY-GEN-DOC---> Generating document for {signee.Id}");

            return (signeeId: signee.Id, documentName: $"{Guid.NewGuid()}.pdf");
        }
        
        [FunctionName("NotifiySignee")]
        public static async Task<Signee> RunNotifiySigneeActivity([ActivityTrigger] Signee signee, ILogger log)
        {
            log.LogInformation($"---DUMMY-SEND-EMAIL---> Notifying signee via email {signee.Email}");
            
            return signee;
        }
    }
}