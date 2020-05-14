using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BlazorQuiz.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;

namespace BlazorQuiz.Functions
{
    public static class ExamFunctions
    {
        private static readonly string CollectionId = "AnswerLog";

        [FunctionName("GetSampleAnswer")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            var question = new Question()
            {
                Id=1,
                Tag = "Physics",
                Title = "What is the letter of the Oxygen element?",
                Choices = new []
                {
                    new Choice() {Text = "N", IsCorrectChoice = false}, 
                    new Choice() {Text = "O", IsCorrectChoice = true}, 
                    new Choice() {Text = "C", IsCorrectChoice = false}, 
                }
            };

            var answer = new Answer();
            answer.CandidateId = Guid.NewGuid().ToString("N");
            answer.Question = question;
            answer.Choices = question.GetAnswerChoices().ToList();
            answer.AnsweredAt = DateTime.UtcNow;

            return new OkObjectResult(answer);
        }

        [FunctionName("setup")]
        public static async Task<IActionResult> Setup(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var dbName = config["COSMOSDB_DBNAME"];

            using (var client = new DocumentClient(new Uri(config["COSMOSDB_URI"]), config["COSMOSDB_KEY"]))
            {
                var response = await client.CreateDatabaseIfNotExistsAsync(new Database { Id = dbName });
                var db = response.Resource;

                DocumentCollection collectionDefinition = new DocumentCollection();
                collectionDefinition.Id = CollectionId;
                collectionDefinition.PartitionKey.Paths.Add("/AnswerId");

                DocumentCollection answerLogCollection = await client.CreateDocumentCollectionIfNotExistsAsync(
                    UriFactory.CreateDatabaseUri(dbName),
                    collectionDefinition);
            }

            return new OkObjectResult(new { Status = "Ok" });
        }

        [FunctionName("answer")]
        public static async Task<IActionResult> CollectAnswer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "answer/{candidateId}")] Answer req,
                string candidateId,
                ILogger log,
                ExecutionContext context)
        {
            log.LogInformation($"New answer for {candidateId} : {req?.Question?.Title}");

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var dbName = config["COSMOSDB_DBNAME"];

            using (var client = new DocumentClient(new Uri(config["COSMOSDB_URI"]), config["COSMOSDB_KEY"]))
            {
                Uri collectionUri = UriFactory.CreateDocumentCollectionUri(dbName, CollectionId);
                await client.CreateDocumentAsync(collectionUri, req);
            }

            return new OkObjectResult(new {Status = "Ok"});
        }
    }
}
