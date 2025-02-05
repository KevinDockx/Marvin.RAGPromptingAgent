using Marvin.RAGPromptingAgent.Models;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using System.Text;
using System.Text.Json;

// Fill out your own values here.  Have a look at the included
// readme.md to learn how to configure these values.
const string gitHubAppName = "kevindockx-copilot-rag-ai-agent";
const string gitHubCopilotApiBaseAddress = "https://api.githubcopilot.com/";
const string guidelineRepositoryOwner = "KevinDockx";
const string guidelineRepositoryName = "Marvin.RAGPromptingAgent";
const string guidelineRepositoryBranch = "main";
const string guidelineFilePath = "CodingGuidelinesByExample.txt";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped(sp =>
{
    var productInformation = new ProductHeaderValue(gitHubAppName);
    return new GitHubClient(productInformation);
});

builder.Services.AddHttpClient("GitHubCopilotApiClient", client =>
{
    client.BaseAddress = new Uri(gitHubCopilotApiBaseAddress);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.MapGet("/hello", () => "Hi there, I'm your friendly neighbourhood Copilot Agent");

app.MapGet("/authentication-callback", () => "Lookin' good.");

app.MapPost("/incoming-copilot-message", async (
    [FromHeader(Name = "X-GitHub-Token")] string gitHubApiToken,
    [FromServices] GitHubClient gitHubClient,
    [FromServices] IHttpClientFactory httpClientFactory,
    HttpRequest request) =>
{
    gitHubClient.Credentials = new Credentials(gitHubApiToken);

    // Don't use [FromBody] to deserialize the incoming payload as the incoming stream can,
    // by default, only be read once, and we need the raw input for signature validation (TO BE IMPLEMENTED).
    // To avoid having to create workarounds (like working with a copy of the incoming stream), 
    // or run the risk of serializer/encoding-settings interfering with the incoming
    // payload, read the request body manually.

    string incomingCopilotPayloadAsString = string.Empty;
    IncomingCopilotPayload incomingCopilotPayload = new ();
    request.EnableBuffering();
    using (var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true))
    {
        incomingCopilotPayloadAsString = await reader.ReadToEndAsync();
        // Be a good citizen and reset the position for whatever may be next in the pipeline
        request.Body.Position = 0;
    }

    incomingCopilotPayload = JsonSerializer.Deserialize<IncomingCopilotPayload>(
        incomingCopilotPayloadAsString,
        JsonSerializerOptions.Web)
            ?? throw new ArgumentException("Incoming payload is empty.");

    // Read the coding guidelines.  Note that you can also read multiple files or even an
    // entire codebase to use as guidelines if needed.  Keep in mind that the more you read, 
    // the more tokens you'll send to the LLM.  This may incur costs if your LLM subscription
    // is based on the amount of tokens sent.  
    var guidelineContents = await gitHubClient.Repository.Content.GetAllContentsByRef(
        guidelineRepositoryOwner,
        guidelineRepositoryName,
        guidelineFilePath,
        guidelineRepositoryBranch);

    var guidelineContent = guidelineContents.Count > 0 ? guidelineContents[0].Content : string.Empty;

    var outgoingCopilotPayload = new OutgoingCopilotPayload
    {
        Messages = incomingCopilotPayload.Messages.Concat(
        new[]
        {
                new CopilotMessage
                {
                    Role = "system",
                    Content = guidelineContent
                }
        }).ToList(),
        Stream = true
    };

    var httpClient = httpClientFactory.CreateClient("GitHubCopilotApiClient");
    httpClient.DefaultRequestHeaders.Authorization = new("Bearer", gitHubApiToken);

    var copilotResponseMessage = await httpClient.PostAsJsonAsync(
        "chat/completions",
        outgoingCopilotPayload);

    copilotResponseMessage.EnsureSuccessStatusCode();

    return Results.Stream(
        await copilotResponseMessage.Content.ReadAsStreamAsync(),
        "application/json");
});
 
app.Run();
