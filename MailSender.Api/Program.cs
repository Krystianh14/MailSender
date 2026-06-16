using Microsoft.OpenApi.Models;
using MailSender.Application.Interfaces;
using MailSender.Application.Services;
using MailSender.Application.Settings;
using MailSender.Infrastructure.Auth;
using MailSender.Infrastructure.MailProviders;
using MailSender.Infrastructure.Repositories;
using MailSender.Infrastructure.Registration;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;



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
builder.Services.Configure<RegistrationSettings>(
    builder.Configuration.GetSection("Registration")
);
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt")
);
builder.Services.Configure<StudentSettings>(
    builder.Configuration.GetSection("Student")
);
builder.Services.Configure<BrevoSettings>(
    builder.Configuration.GetSection("Brevo")
);
builder.Services.AddScoped<IRegistrationPasswordValidator, RegistrationPasswordValidator>();

builder.Services.AddSingleton<IClientApplicationRepository, InMemoryClientApplicationRepository>();

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IMailSenderProvider, FakeMailSenderProvider>();

builder.Services.AddScoped<ClientApplicationService>();
builder.Services.AddScoped<MailService>();
builder.Services.AddHttpClient<IMailSenderProvider, BrevoMailSenderProvider>();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();