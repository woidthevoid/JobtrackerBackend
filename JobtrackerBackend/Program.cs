using Supabase;
using JobtrackerBackend;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Load from configuration/environment
var supabaseUrl = builder.Configuration["Supabase:Url"];
var supabaseKey = builder.Configuration["Supabase:Key"];
var jwtSecret = builder.Configuration["Supabase:ServiceRoleKey"];

if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey) || string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException("Supabase URL, Key, or JWT Secret is not configured. Please check appsettings.json or environment variables.");
}

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

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "supabase",
        ValidAudience = "authenticated",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("Token validated successfully.");
            foreach (var claim in context.Principal.Claims)
            {
                Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
            }

            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"Authentication challenge: {context.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.WebHost.UseUrls("http://localhost:5050");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/jobs", async (Supabase.Client client, ClaimsPrincipal user) =>
{
    if (!user.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    var userId = user.GetUserId();
    if (userId == null)
    {
        Console.WriteLine("User ID not found in token claims");
        return Results.Forbid();
    }
    
    try
    {
        var result = await client.From<JobApplication>()
            .Where(j => j.UserId == userId)
            .Get();
        var jobs = result.Models;

        var jobDtos = jobs.Select(job => new JobApplicationDto
        {
            Id = job.Id,
            UserId = job.UserId,
            Title = job.Title,
            Description = job.Description,
            JobLink = job.JobLink,
            CvFileUrl = job.CvFileUrl,
            AppFileUrl = job.AppFileUrl,
            ApplicationStatus = job.ApplicationStatus,
            CreatedAt = job.CreatedAt,
        }).ToList();
        
        return Results.Ok(jobDtos);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching for user {userId}: {ex.Message}");
        return Results.Problem("Internal error: " + ex.Message);
    }
})
.RequireAuthorization();

app.MapDelete("/jobs/{id}", async (Guid id, Supabase.Client client, ClaimsPrincipal user) =>
{
    if (!user.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    var userId = user.GetUserId();
    if (userId == null)
    {
        Console.WriteLine("User ID not found in token claims for delete operation");
        return Results.Forbid();
    }
    
    try
    {
        var jobDelete = await client.From<JobApplication>()
            .Where(j => j.Id == id && j.UserId == userId)
            .Get();

        if (!jobDelete.Models.Any())
        {
            return Results.NotFound($"Job with id {id} not found or does not belong to user");
        }

        await client.From<JobApplication>()
            .Where(j => j.Id == id)
            .Delete();
        
        return Results.Ok($"Job with id {id} deleted successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error deleting job with id {id}: {ex.Message}");
        return Results.Problem("Internal error: " + ex.Message);
    }
})
.RequireAuthorization();

app.MapPost("/jobs", async ([FromForm] CreateJobApplicationViewModel model, Supabase.Client client, ClaimsPrincipal user) =>
{
    if (!user.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }
    
    var userId = user.GetUserId();
    if (userId == null)
    {
        Console.WriteLine("User ID not found in token claims for post operation");
        return Results.Forbid();
    }
    
    try
    {
        if (string.IsNullOrEmpty(model.Title) || string.IsNullOrEmpty(model.Description))
        {
            return Results.BadRequest("Title or description is requiered");
        }

        string? cvFileUrl = null;
        string? appFileUrl = null;
        string bucketName = "job-application-files";

        if (model.CvFile != null && model.CvFile.Length > 0)
        {
            var fileName = $"{Guid.NewGuid()}_{model.CvFile.FileName}";
            var cvFilePath = $"cvs/{userId}/{fileName}";

            using var stream = new MemoryStream();
            await model.CvFile.CopyToAsync(stream);
            stream.Position = 0;

            await client.Storage.From(bucketName).Upload(stream.ToArray(), cvFilePath);
            cvFileUrl = client.Storage.From(bucketName).GetPublicUrl(cvFilePath);
        }

        if (model.AppFile != null && model.AppFile.Length > 0)
        {
            var appFileName = $"{Guid.NewGuid()}_{model.AppFile.FileName}";
            var appFilePath = $"apps/{userId}/{appFileName}";

            using var stream = new MemoryStream();
            await model.AppFile.CopyToAsync(stream);
            stream.Position = 0;

            await client.Storage.From(bucketName).Upload(stream.ToArray(), appFilePath);
            appFileUrl = client.Storage.From(bucketName).GetPublicUrl(appFilePath);
        }

        var newJob = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            Title = model.Title,
            Description = model.Description,
            JobLink = model.JobLink,
            CvFileUrl = cvFileUrl,
            AppFileUrl = appFileUrl,
            ApplicationStatus = model.ApplicationStatus,
            CreatedAt = DateTime.UtcNow,
        };

        var dbResponse = await client.From<JobApplication>().Insert(newJob);
        var insertedJob = dbResponse.Models.FirstOrDefault();

        if (insertedJob == null)
        {
            return Results.Problem("Failed to insert");
        }

        var insertedJobDto = new JobApplicationDto
        {
            Id = insertedJob.Id,
            UserId = insertedJob.UserId,
            Title = insertedJob.Title,
            Description = insertedJob.Description,
            JobLink = insertedJob.JobLink,
            CvFileUrl = insertedJob.CvFileUrl,
            AppFileUrl = insertedJob.AppFileUrl,
            ApplicationStatus = insertedJob.ApplicationStatus,
            CreatedAt = insertedJob.CreatedAt,
        };

        return Results.Created($"/jobs/{insertedJobDto.Id}", insertedJobDto);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing job creation with file upload: {ex.Message}");
        return Results.Problem("Internal error during job creation or file upload: " + ex.Message);
    }
})
.RequireAuthorization();

app.Run();