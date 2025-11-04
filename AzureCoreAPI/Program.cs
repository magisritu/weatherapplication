using AzureCoreAPI.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Auth using Azure AD
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddMicrosoftIdentityWebApi(options =>
                        {
                            configuration.Bind("AzureAd", options);
                            options.Events = new JwtBearerEvents();

                            options.Events = new JwtBearerEvents
                            {
                                OnTokenValidated = context =>
                                {
                                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

                                    // Access the scope claim (scp) directly
                                    var scopeClaim = context.Principal?.Claims.FirstOrDefault(c => c.Type == "scp")?.Value;

                                    if (scopeClaim != null)
                                    {
                                        logger.LogInformation("Scope found in token: {Scope}", scopeClaim);
                                    }
                                    else
                                    {
                                        logger.LogWarning("Scope claim not found in token.");
                                    }

                                    return Task.CompletedTask;
                                },
                                OnAuthenticationFailed = context =>
                                {
                                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                                    logger.LogError("Authentication failed: {Message}", context.Exception.Message);
                                    return Task.CompletedTask;
                                },
                                OnChallenge = context =>
                                {
                                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                                    logger.LogError("Challenge error: {ErrorDescription}", context.ErrorDescription);
                                    return Task.CompletedTask;
                                }
                            };
                        }, options => { configuration.Bind("AzureAd", options); });

// The following flag can be used to get more descriptive errors in development environments
IdentityModelEventSource.ShowPII = false;


// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDataServices(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowSpecificOrigin",
                      builder =>
                      {
                          builder.WithOrigins(["https://rituraj-angular-d8h7eccubkech2ae.eastasia-01.azurewebsites.net", "http://localhost:4200"]) // Replace with your Angular app's origin
                                 .AllowAnyHeader()
                                 .AllowAnyMethod();
                      });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("AllowSpecificOrigin");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
