# ThreatModelGPT
 
ThreatModelGPT is a C# program that uses Azure's Computer Vision and Azure OpenAI's GPT-4 to generate attack trees for AWS services.

## Prerequisites
 
.NET Core 3.1 or later
Azure Computer Vision API key and endpoint
OpenAI GPT-3 API key and endpoint
An image file with text

## Setup
 
Clone the repository to your local machine.
Create a .env file in the root directory of the project.
Add the following environment variables to the .env file:

OPENAI_API_KEY=<Your OpenAI API Key>  
OPENAI_API_ENDPOINT=<Your OpenAI API Endpoint>  
COMPUTER_VISION_API_KEY=<Your Azure Computer Vision API Key>  
COMPUTER_VISION_API_ENDPOINT=<Your Azure Computer Vision API Endpoint>  
IMAGE_FILEPATH=<Path to your image file>  
 

## Usage
 
Run the program with the following command:

dotnet run  
 
The program will extract text from the provided image, identify AWS services mentioned in the text, generate a list of potential security threats for each service, and generate a list of security recommendations for each threat. This progam will also assist in create Attack trees for the services provided.

## Contributing
 
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.
License
 