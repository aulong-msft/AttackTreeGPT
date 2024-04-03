﻿using Azure;
using Azure.AI.OpenAI;
using DotNetEnv;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Octokit;
using Azure.Identity;
using System.Net.Http;
using System.Threading.Tasks;


namespace ThreatModelGPT
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Load environment variables from .env file
            Env.Load();
            
            // Assign values from environment variables
            string openAiApiKey = Env.GetString("OPENAI_API_KEY");
            string openAiApiendpoint = Env.GetString("OPENAI_API_ENDPOINT");
            string computerVisionApiKey = Env.GetString("COMPUTER_VISION_API_KEY");
            string computerVisionApiEndpoint = Env.GetString("COMPUTER_VISION_API_ENDPOINT");
            string imageFilePath = Env.GetString("IMAGE_FILEPATH");
            string githubUsername = Env.GetString("GITHUB_USERNAME");
            string githubPersonalAccessToken = Env.GetString("GITHUB_PERSONAL_ACCESS_TOKEN");
  
            // Create a computer vision client to obtain text from a provided image
            ComputerVisionClient client = Authenticate(computerVisionApiEndpoint, computerVisionApiKey);

            // Extract text (OCR) from the provided local image file
            var extractedText = await ReadLocalImage(client, imageFilePath);

            Console.WriteLine("Extracted Text from Image:");
            Console.WriteLine(extractedText);

            // Use OpenAI API to generate intelligible keywords from the extracted text
            var listOfServices = await GenerateListOfServices(extractedText, openAiApiKey, openAiApiendpoint);

            // Use OpenAI API to generate recommendations from the extracted text
            string concatenatedString = string.Join(",", listOfServices); // Using a space as delimiter

            // Use OpenAI API to generate threats from the extracted text

            var threats =GenerateListOfSecurityThreats(concatenatedString, openAiApiKey, openAiApiendpoint);
            
            // Use OpenAI API to generate recommendations from the extracted text
            var recommendations = await GenerateListOfSecurityRecommendations(concatenatedString, openAiApiKey, openAiApiendpoint);

         /*   List<string> securityBaselines = await GetSecurityBaselinesAsync(concatenatedString, githubUsername, githubPersonalAccessToken);
            
            foreach (string baseline in securityBaselines)
            {
                if (!string.IsNullOrWhiteSpace(baseline)) // Check if the baseline is not null, empty, or whitespace
                {
                    Console.WriteLine("SECURITY BASELINE FOR:" + baseline);
                }
            }*/
        }

        public static ComputerVisionClient Authenticate(string endpoint, string key)
        {
            ComputerVisionClient client =
                new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
                { Endpoint = endpoint };
            return client;
        }

        public static async Task<string> ReadLocalImage(ComputerVisionClient client, string imagePath)
        {
            Console.WriteLine("----------------------------------------------------------");
            Console.WriteLine("READ LOCAL IMAGE");
            Console.WriteLine();

            // Read image data
            byte[] imageData = File.ReadAllBytes(imagePath);

            // Read text from image data
            var textHeaders = await client.ReadInStreamAsync(new MemoryStream(imageData));
            
            // After the request, get the operation location (operation ID)
            string operationLocation = textHeaders.OperationLocation;
            Thread.Sleep(2000);

            // Retrieve the URI where the extracted text will be stored from the Operation-Location header.
            // We only need the ID and not the full URL
            const int numberOfCharsInOperationId = 36;
            string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);

            // Extract the text
            ReadOperationResult results;
            Console.WriteLine($"Extracting text from local image {Path.GetFileName(imagePath)}...");
            Console.WriteLine();

            do
            {
                results = await client.GetReadResultAsync(Guid.Parse(operationId));
            }
            while (results.Status == OperationStatusCodes.Running || results.Status == OperationStatusCodes.NotStarted);

            // Store and return the extracted text
            var extractedText = "";
            Console.WriteLine();
            var textUrlFileResults = results.AnalyzeResult.ReadResults;
            
            foreach (ReadResult page in textUrlFileResults)
            {
                foreach (Line line in page.Lines)
                {
                    extractedText += line.Text + "\n";
                }
            }
            return extractedText;
        }

       public static async Task<List<string>> GenerateListOfServices(string text, string apiKey, string apiEndpoint)
        {
            string engine = "text-davinci-003";
            List<string> recommendations = new List<string>(); 
            string prompt = $"Prompt 1: You are an Amazon AWS security engineer doing threat model analysis to identify and mitigate risk. Given the following text:\n{text}\n please find the relevant AWS Services and print them out. \n";

            OpenAIClient client = new OpenAIClient(new Uri(apiEndpoint), new AzureKeyCredential(apiKey));

            // Prompt tuning parameters
            Console.Write($"Input: {prompt}");
            CompletionsOptions completionsOptions = new CompletionsOptions();
            completionsOptions.Prompts.Add(prompt);
            completionsOptions.MaxTokens = 500;
            completionsOptions.Temperature = 0.2f;

            Response<Completions> completionsResponse = client.GetCompletions(engine, completionsOptions);
            string completion = completionsResponse.Value.Choices[0].Text;
            Console.WriteLine($"Chatbot: {completion}");
            recommendations.Add(completion); // Add the completion to the list

            return recommendations; 
        }
       public static async Task<List<string>> GenerateListOfSecurityRecommendations(string text, string apiKey, string apiEndpoint)
        {
            
            string engine = Env.GetString("CHAT_ENGINE");
            List<string> recommendations = new List<string>();
            string prompt =
                "Prompt 2:\n" +
                "As a Amazon AWS security engineer specializing in threat model analysis and risk mitigation, you have been tasked with evaluating the security posture of various AWS services:\n" +
                $"{text}\n" +
                "Your objective is to identify service-specific security recommendations by leveraging AWS Security Basline documentation and AWS to find tailored security advice for each service.\n";


            OpenAIClient client = new OpenAIClient(new Uri(apiEndpoint), new AzureKeyCredential(apiKey));

            // Prompt tuning parameters
            Console.Write($"Input: {prompt}");
            CompletionsOptions completionsOptions = new CompletionsOptions();
            completionsOptions.Prompts.Add(prompt);
            completionsOptions.MaxTokens = 2000;
            completionsOptions.Temperature = 0.5f;
            completionsOptions.NucleusSamplingFactor = 0.5f;

            Response<Completions> completionsResponse = client.GetCompletions(engine, completionsOptions);
            string completion = completionsResponse.Value.Choices[0].Text;
            Console.WriteLine($"Chatbot: {completion}");
            recommendations.Add(completion); // Add the completion to the list

            return recommendations;
        }

        public static async Task<List<string>> GenerateListOfSecurityThreats(string text, string apiKey, string apiEndpoint)
        {
            
            string engine = Env.GetString("CHAT_ENGINE");
            List<string> recommendations = new List<string>();
            string prompt =
                "Prompt 2:\n" +
                "As a Amazon AWS security engineer specializing in threat model analysis and risk mitigation, you have been tasked with evaluating the security posture of various Amazon AWS services:\n" +
                $"{text}\n" +
                "Your objective is to identify service-specific security threats by leveraging Microsoft STRIDE threat modeling framework documentation and Microsoft docs to find tailored security advice for each service.\n";


            OpenAIClient client = new OpenAIClient(new Uri(apiEndpoint), new AzureKeyCredential(apiKey));

            // Prompt tuning parameters
            Console.Write($"Input: {prompt}");
            CompletionsOptions completionsOptions = new CompletionsOptions();
            completionsOptions.Prompts.Add(prompt);
            completionsOptions.MaxTokens = 2000;
            completionsOptions.Temperature = 0.5f;
            completionsOptions.NucleusSamplingFactor = 0.5f;

            Response<Completions> completionsResponse = client.GetCompletions(engine, completionsOptions);
            string completion = completionsResponse.Value.Choices[0].Text;
            Console.WriteLine($"Chatbot: {completion}");
            recommendations.Add(completion); // Add the completion to the list

            return recommendations;
        }
        /*static async Task<List<string>> GetSecurityBaselinesAsync(string services, string username, string personalAccessToken)
        {
            string owner = "MicrosoftDocs";
            string repo = "SecurityBenchmarks";
            string path = "Azure Offer Security Baselines/3.0";
            string path2 = "Microsoft Cloud Security Benchmark/";
            string path3 = "Azure Security Benchmark/3.0";

            var client = new GitHubClient(new ProductHeaderValue("MyApp"));
            var basicAuth = new Credentials(username, personalAccessToken); 
            client.Credentials = basicAuth;

            List<string> securityBaselines = new List<string>();
            string[] individualServices = services.Split(',').Select(s => s.Trim()).ToArray();

            // Find a security baseline for each recommendation from the OpenAI API
            foreach (string individualService in individualServices)
            {
                // todo: add services to replace invalid strings

                try
                {
                    string trimmedService = individualService.Trim();
                    string content = await GetFileContentAsync(client, owner, repo, path, trimmedService);
                    securityBaselines.Add(content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to fetch data for service: {individualService}, Error: {ex.Message}");
                }
            }

            // Find the security baseline for the entire Azure Security Benchmark
            try
            {
                string content = await GetFileContentAsync(client, owner, repo, path3, "Azure Security Benchmark");
                securityBaselines.Add(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch data for Azure Security Benchmark, Error: {ex.Message}");
            }

            // Find the security baseline for the entire Microsoft Cloud Security Benchmark
            try
            {
                string content = await GetFileContentAsync(client, owner, repo, path2, "Microsoft_cloud_security_benchmark");
                securityBaselines.Add(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch data for Microsoft Cloud Security Benchmark, Error: {ex.Message}");
            }

            return securityBaselines;
        }*/
                
        static async Task<string> GetFileContentAsync(GitHubClient client, string owner, string repo, string path, string service)
        {
            var contents = await client.Repository.Content.GetAllContentsByRef(owner, repo, path, "master");
                        
            foreach (var content in contents)
            {
                // Replace spaces with hyphens to match filename format
                string formattedService = service.Replace(" ", "-").Replace(".", "").ToLower();
                
                if (content.Name.IndexOf(formattedService, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Construct the URL to the baseline file
                    string baselineUrl = $" {service}: https://github.com/{owner}/{repo}/blob/master/{content.Path}";

                    return baselineUrl;
                }
            }

            // return empty string if the list isnt populated
           return "";
        }
    }
}  