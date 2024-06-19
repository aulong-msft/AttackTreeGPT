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
            var threats = await GenerateListOfSecurityThreats(concatenatedString, openAiApiKey, openAiApiendpoint);
            
            // Use OpenAI API to generate recommendations from the extracted text
            Console.WriteLine("Generating Recommendations from the extracted text");
            var recommendations = await GenerateListOfAttackTrees(concatenatedString, openAiApiKey, openAiApiendpoint);
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
            string engine = Env.GetString("CHAT_ENGINE");
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
       public static async Task<List<string>> GenerateListOfAttackTrees(string text, string apiKey, string apiEndpoint)
        {
            
            string engine = Env.GetString("CHAT_ENGINE");
            List<string> recommendations = new List<string>();
            string prompt =
                "Prompt 3:\n" +
                "As a Amazon AWS security engineer specializing in threat model analysis you are creating attack trees for each of the aws services provided\n" +
                $"{text}\n" +
                "Ensure each attack tree has the perspective of an adversary trying to hack into the system, you set a clear adversarial goal node and leaf nodes \n";


            OpenAIClient client = new OpenAIClient(new Uri(apiEndpoint), new AzureKeyCredential(apiKey));

            // Prompt tuning parameters
            Console.Write($"Input: {prompt}");
            CompletionsOptions completionsOptions = new CompletionsOptions();
            completionsOptions.Prompts.Add(prompt);
            completionsOptions.MaxTokens = 7000;
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
                "Your objective is to identify unique service-specific security threats with the list of services provided.\n";


            OpenAIClient client = new OpenAIClient(new Uri(apiEndpoint), new AzureKeyCredential(apiKey));

            // Prompt tuning parameters
            Console.Write($"Input: {prompt}");
            CompletionsOptions completionsOptions = new CompletionsOptions();
            completionsOptions.Prompts.Add(prompt);
            completionsOptions.MaxTokens = 7000;
            completionsOptions.Temperature = 0.5f;
            completionsOptions.NucleusSamplingFactor = 0.5f;

            Response<Completions> completionsResponse = client.GetCompletions(engine, completionsOptions);
            string completion = completionsResponse.Value.Choices[0].Text;
            Console.WriteLine($"Chatbot: {completion}");
            recommendations.Add(completion); // Add the completion to the list

            return recommendations;
        }

    }
}  