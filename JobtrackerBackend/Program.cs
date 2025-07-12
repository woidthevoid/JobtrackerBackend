using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using JobtrackerBackend;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Supabase;
using Supabase.Postgrest;
using Client = Supabase.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddAuthorization();

builder.Services.AddHttpClient();

builder.Services.AddHttpContextAccessor();

var bytes = Encoding.UTF8.GetBytes(builder.Configuration["Supabase:JwtSecret"]!);

builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(bytes),
        ValidAudience = builder.Configuration["Auth:validAudience"],
        ValidIssuer = builder.Configuration["Auth:Issuer"],
    };
});

builder.Services.AddScoped( provider =>
{
    var supabaseUrl = builder.Configuration["Supabase:Url"];
    var supabaseKey = builder.Configuration["Supabase:Key"];
    var options = new SupabaseOptions { AutoConnectRealtime = false };

    var client = new Client(supabaseUrl, supabaseKey, options);
    client.InitializeAsync().Wait();
    return client;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

static Guid? GetUserIdFromJwt(string jwt)
{
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(jwt);
    if (Guid.TryParse(token.Subject, out var userId))
        return userId;
    return null;
}

app.MapGet("/jobs", async (
    IHttpContextAccessor httpContextAccessor,
    IHttpClientFactory httpClientFactory,
    IConfiguration config
) =>
{
    var request = httpContextAccessor.HttpContext?.Request;
    var token = request?.Headers["Authorization"].ToString().Replace("Bearer ", "");
    Console.WriteLine("Token" + token);

    if (string.IsNullOrWhiteSpace(token))
    {
        return Results.Unauthorized();
    }

    var supabaseUrl = config["Supabase:Url"];
    var client = httpClientFactory.CreateClient();
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    client.DefaultRequestHeaders.Add("apiKey", config["Supabase:Key"]);

    var url = $"{supabaseUrl}/rest/v1/job_applications?select=*";

    try
    {
        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Failed to fetch jobs. Status: {response.StatusCode}");
            return Results.StatusCode((int)response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync();
        return Results.Content(json, "application/json");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception while fetching jobs: {ex.Message}");
        return Results.Problem("Could not fetch job applications.");
    }
}).RequireAuthorization();

app.MapPost("/jobs", async (
    IHttpContextAccessor httpContextAccessor,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    Client supabase, // 👈 injected Supabase Client
    [FromForm] CreateJobApplicationViewModel formData
) =>
{
    var userIdClaim = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var clientUserId))
    {
        return Results.Unauthorized();
    }
    var token = await httpContextAccessor.HttpContext!.GetTokenAsync("access_token");

    if (string.IsNullOrWhiteSpace(token))
        return Results.Unauthorized();

    var dummyRefreshToken = "dummy_refresh_token_for_supabase_net"; 

    await supabase.Auth.SetSession(token, dummyRefreshToken, false);

    var userId = GetUserIdFromJwt(token);
    if (userId == null)
        return Results.Unauthorized();

    var supabaseUrl = config["Supabase:Url"];
    var client = httpClientFactory.CreateClient();
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    client.DefaultRequestHeaders.Add("apiKey", config["Supabase:Key"]);

    async Task<string?> UploadFile(IFormFile file, string bucket, string path)
    {
        var stream = file.OpenReadStream();
        var content = new StreamContent(stream);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

        var response = await client.PutAsync($"{supabaseUrl}/storage/v1/object/{bucket}/{path}", content);
        return response.IsSuccessStatusCode
            ? $"{supabaseUrl}/storage/v1/object/public/{bucket}/{path}"
            : null;
    }

    string? cvFileUrl = null;
    string? appFileUrl = null;

    if (formData.CvFile != null)
    {
        var path = $"{userId}/{formData.CvFile.FileName}";
        cvFileUrl = await UploadFile(formData.CvFile, "job-application-files", path);
    }

    if (formData.AppFile != null)
    {
        var path = $"{userId}/{formData.AppFile.FileName}";
        appFileUrl = await UploadFile(formData.AppFile, "job-application-files", path);
    }

    var job = new JobApplication
    {
        Id = Guid.NewGuid(),
        Title = formData.Title,
        Description = formData.Description,
        JobLink = formData.JobLink,
        CvFileUrl = cvFileUrl ?? string.Empty,
        AppFileUrl = appFileUrl ?? string.Empty,
        ApplicationStatus = string.IsNullOrWhiteSpace(formData.ApplicationStatus)
        ? "applied"
        :formData.ApplicationStatus,
        CreatedAt = DateTime.Now,
        // 👇 this assumes you add UserId property to your model
        UserId = userId.Value
    };

    try
    {
        var result = await supabase.From<JobApplication>().Insert(job, new QueryOptions
        {
            Returning = QueryOptions.ReturnType.Representation
        });

        var inserted = result.Models.First();

        return Results.Ok(new JobApplicationDto
        {
            Id = inserted.Id,
            Title = inserted.Title,
            Description = inserted.Description,
            JobLink = inserted.JobLink,
            CvFileUrl = inserted.CvFileUrl,
            AppFileUrl = inserted.AppFileUrl,
            ApplicationStatus = inserted.ApplicationStatus,
            CreatedAt = inserted.CreatedAt
        });
    }
    catch (Exception ex)
    {
        return Results.Problem("Error inserting job: " + ex.Message);
    }
})
.Accepts<CreateJobApplicationViewModel>("multipart/form-data")
.Produces<JobApplicationDto>()
.DisableAntiforgery()
.RequireAuthorization();

app.Run();
