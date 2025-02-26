using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

string filePath = Path.GetFullPath("../appsettings.json");
var config = new ConfigurationBuilder()
    .AddJsonFile(filePath)
    .Build();

// Set your values in appsettings.json
string modelId = config["modelId"]!;
string endpoint = config["endpoint"]!;
string apiKey = config["apiKey"]!;

// Create a kernel with Azure OpenAI chat completion
var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);

// Build the kernel
Kernel kernel = builder.Build();
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// Solution code
kernel.ImportPluginFromType<CurrencyConverterPlugin>();
kernel.ImportPluginFromType<FlightBookingPlugin>();
OpenAIPromptExecutionSettings promptExecutionSettings = new() 
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

string hbprompt = """
    <message role="system">Instructions: Before providing the the user with a travel itenerary, ask how many days their trip is</message>
    <message role="user">I'm going to {{city}}. Can you create an itinerary for me?</message>
    <message role="assistant">Sure, how many days is your trip?</message>
    <message role="user">{{input}}</message>
    <message role="assistant">
    """;

var templateFactory = new HandlebarsPromptTemplateFactory();
var promptTemplateConfig = new PromptTemplateConfig()
{
    Template = hbprompt,
    TemplateFormat = "handlebars",
    Name = "CreateItinerary",
};

var function = kernel.CreateFunctionFromPrompt(promptTemplateConfig, templateFactory);
var plugin = kernel.CreatePluginFromFunctions("TravelItinerary", [function]);
kernel.Plugins.Add(plugin);

var history = new ChatHistory();
history.AddSystemMessage("Before providing destination recommendations, ask the user if they have a budget for their trip.");

Console.WriteLine("Press enter to exit");
Console.WriteLine("Assistant: How may I help you?");
Console.Write("User: ");

string input = Console.ReadLine()!;
while (input != "") 
{
    history.AddUserMessage(input);
    await GetReply();
    Console.Write("User: ");
    input = Console.ReadLine()!;
}

async Task GetReply() 
{
    ChatMessageContent reply = await chatCompletionService.GetChatMessageContentAsync(
        history,
        executionSettings: promptExecutionSettings,
        kernel: kernel
    );

    Console.WriteLine("Assistant: " + reply.ToString());
    history.AddAssistantMessage(reply.ToString());
}