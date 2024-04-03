# Threat Model GPT

The **Threat Model GPT** project is a console application that uses Azure Cognitive Services, OpenAI, and GitHub API to perform threat modeling analysis on a provided image, extract keywords, generate security recommendations, and fetch security baseline information from a GitHub repository.

## Overview

This application performs the following steps:

1. **Image Text Extraction:** Uses Azure Cognitive Services to extract text from a provided image.

2. **Keyword Generation:** Uses OpenAI API to generate keywords from the extracted text relevant to Azure services.

3. **Security Recommendations:** Uses OpenAI API to generate actionable security practices tailored to each service mentioned in the text.

4. **Security Baselines:** Fetches Azure security baseline information from a GitHub repository based on the generated keywords.

## Getting Started

1. Clone the repository and navigate to the project directory.

2. Create a `.env` file in the project root and provide the following environment variables:

   - `OPENAI_API_KEY`: Your OpenAI API key.
   - `OPENAI_API_ENDPOINT`: OpenAI API endpoint.
   - `COMPUTER_VISION_API_KEY`: Your Azure Cognitive Services Computer Vision API key.
   - `COMPUTER_VISION_API_ENDPOINT`: Azure Cognitive Services Computer Vision API endpoint.
   - `IMAGE_FILEPATH`: Path to the image for text extraction.
   - `GITHUB_USERNAME`: Your GitHub username.
   - `GITHUB_PERSONAL_ACCESS_TOKEN`: Your GitHub personal access token.

3. Build and run the application using the `dotnet` command:

   ```bash
   dotnet build
   dotnet run

## Dependencies

Azure.AI.OpenAI
Microsoft.Azure.CognitiveServices.Vision.ComputerVision
Octokit
DotNetEnv
License
This project is licensed under the MIT License.

## Notes

This project is intended for educational and demonstration purposes only.
Ensure that you follow best practices for managing API keys and tokens.
For more detailed information about the code and its functionality, refer to the source code comments.

To authorize the PAT token, go to "Develper Settings", generate the PAT token, then right click on the token and hit "Authorize" to the "microsoft" organization. this is to access the Microsoft cloud security bechmark repository.