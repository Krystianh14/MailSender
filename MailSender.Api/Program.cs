using Microsoft.OpenApi.Models;
using MailSender.Application.Interfaces;
using MailSender.Application.Services;
using MailSender.Application.Settings;
using MailSender.Infrastructure.Auth;
using MailSender.Infrastructure.MailProviders;
using MailSender.Infrastructure.Repositories;
using MailSender.Infrastructure.Registration;
using MailSender.Infrastructure.Persistence;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClientPolicy", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt")
);
builder.Services.Configure<List<StudentSettings>>(
    builder.Configuration.GetSection("Students")
);
builder.Services.Configure<BrevoSettings>(
    builder.Configuration.GetSection("Brevo")
);
builder.Services.Configure<MailtrapSettings>(
    builder.Configuration.GetSection("Mailtrap")
);
builder.Services.Configure<MailProviderSettings>(
    builder.Configuration.GetSection("MailProvider")
);
builder.Services.AddScoped<IRegistrationPasswordValidator, RegistrationPasswordValidator>();

builder.Services.AddDbContext<MailSenderDbContext>(options =>
{
    options.UseInMemoryDatabase("MailSenderDatabase");
});

builder.Services.AddScoped<IClientApplicationRepository, EfClientApplicationRepository>();
builder.Services.AddScoped<IMailLogRepository, EfMailLogRepository>();

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

var selectedMailProvider = builder.Configuration["MailProvider:SelectedProvider"];

switch (selectedMailProvider)
{
    case "Brevo":
        builder.Services.AddHttpClient<IMailSenderProvider, BrevoMailSenderProvider>();
        break;

    case "Mailtrap":
        builder.Services.AddHttpClient<IMailSenderProvider, MailtrapMailSenderProvider>();
        break;

    case "Fake":
        builder.Services.AddScoped<IMailSenderProvider, FakeMailSenderProvider>();
        break;

    default:
        throw new InvalidOperationException(
            $"Unknown mail provider selected: {selectedMailProvider}. Available providers: Fake, Brevo, Mailtrap."
        );
}

builder.Services.AddScoped<ClientApplicationService>();
builder.Services.AddScoped<MailService>();
builder.Services.AddScoped<MailLogService>();

var jwtSettings = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtSettings>() 
    ?? throw new InvalidOperationException("Jwt settings are missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings!.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey)
            ),

            ValidateLifetime = true
        };
    });

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();
app.UseCors("WebClientPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();