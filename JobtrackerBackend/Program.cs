using Supabase;
using JobtrackerBackend;

var builder = WebApplication.CreateBuilder(args);

// Load from configuration/environment
var supabaseUrl = builder.Configuration["Supabase:Url"] ?? "https://znlyezdoxqdkravzrqhy.supabase.co";
var supabaseKey = builder.Configuration["Supabase:Key"] ?? "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InpubHllemRveHFka3JhdnpycWh5Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDg4NzQ4NjMsImV4cCI6MjA2NDQ1MDg2M30.H6HaVJAdIK1Dxa2OZWyLV6MWhWDDHUneIWbRVSCvsYU";

builder.Services.AddSingleton<Supabase.Client>(_ =>
{
    var options = new Supabase.SupabaseOptions
    {
        AutoConnectRealtime = true
    };

    var client = new Supabase.Client(supabaseUrl, supabaseKey, options);
    client.InitializeAsync().Wait();
    return client;
});

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.WebHost.UseUrls("http://localhost:5050");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// You'll add endpoints like this later
// app.MapPost("/jobs", async (JobApplication job, Supabase.Client client) => { ... });

app.MapGet("/jobs", async (Supabase.Client client) =>
{
    try
    {
        var result = await client.From<JobApplication>().Get();
        var jobs = result.Models;
        return Results.Ok(jobs);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching jobs: {ex.Message}");
        return Results.Problem("Internal error: " + ex.Message);
    }
});

app.MapDelete("/jobs/{id}", async (Guid id, Supabase.Client client) =>
{
    try
    {
        // Delete the job(s) with matching id
        await client.From<JobApplication>()
            .Where(j => j.Id == id)
            .Delete();

        // Optionally, check if the row existed/deleted by querying first if needed

        return Results.Ok($"Job with id {id} deleted successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error deleting job with id {id}: {ex.Message}");
        return Results.Problem("Internal error: " + ex.Message);
    }
});

app.MapPost("/jobs", async (JobApplication newJob, Supabase.Client client) =>
{
    try
    {
        // Insert new job application into Supabase
        var response = await client.From<JobApplication>().Insert(newJob);

        var insertedJob = response.Models.FirstOrDefault();

        if (insertedJob == null)
            return Results.Problem("Failed to insert job application.");

        return Results.Created($"/jobs/{insertedJob.Id}", insertedJob);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error inserting job: {ex.Message}");
        return Results.Problem("Internal error: " + ex.Message);
    }
});

app.Run();