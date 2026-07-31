using KolHaNitzachon.PhoneSystem.Application.Services.Payment;
using Infrastructure.External;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.External;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.IVR;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.Payment;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.Recordings;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.Repositories;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.Voice;
using KolHaNitzachon.PhoneSystem.Infrastructure.Payment;
using KolHaNitzachon.PhoneSystem.Infrastructure.Persistence;
using KolHaNitzachon.PhoneSystem.Infrastructure.Recordings;
using KolHaNitzachon.PhoneSystem.Infrastructure.Repositories;
using KolHaNitzachon.PhoneSystem.Infrastructure.Services.IVR;
using KolHaNitzachon.PhoneSystem.Infrastructure.Services.Voice;
using KolHaNitzachon.PhoneSystem.Infrastructure.SignalWire;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//var solaApiKey = builder.Configuration["Sola:ApiKey"] ?? string.Empty;

//Console.WriteLine(
//    string.IsNullOrWhiteSpace(solaApiKey)
//        ? "Sola API key: NOT LOADED"
//        : "Sola API key: LOADED");

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDirectoryBrowser();

builder.Services.AddDbContext<PhoneSystemDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<IPaymentGatewayService, SolaPaymentGatewayService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IRecipientRepository, RecipientRepository>();

//builder.Services.AddHttpContextAccessor();
builder.Services.Configure<SignalWireSettings>(builder.Configuration.GetSection("SignalWire"));
builder.Services.Configure<SignalWireOptions>(builder.Configuration.GetSection("SignalWire"));
builder.Services.AddScoped<IVoiceService, SignalWireVoiceService>();

builder.Services.Configure<LocalRecordingStorageOptions>(builder.Configuration.GetSection(LocalRecordingStorageOptions.SectionName));
//builder.Services.AddScoped<IRecordingStorage, LocalRecordingStorageService>();
var recordingStorageProvider = builder.Configuration["RecordingStorage:Provider"] ?? "Local";
if (recordingStorageProvider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IRecordingStorage, AzureRecordingStorageService>();
}
else
{
    builder.Services.AddScoped<IRecordingStorage, LocalRecordingStorageService>();
}

var audioPromptProvider = builder.Configuration["AudioPrompts:Provider"] ?? "Local";
if (audioPromptProvider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IAudioPromptUrlProvider, AzureBlobAudioPromptUrlProvider>();
}
else
{
    builder.Services.AddScoped<IAudioPromptUrlProvider, LocalAudioPromptUrlProvider>();
}

builder.Services.AddScoped<INumberAudioComposer, NumberAudioComposer>();
//builder.Services.AddSingleton<INumberAudioComposer, NumberAudioComposer>();
builder.Services.AddSingleton<IIvrCallSessionStore, InMemoryIvrCallSessionStore>();
builder.Services.AddHostedService<IvrSessionCleanupService>();
builder.Services.AddScoped<IMenuRenderer, MenuRenderer>();

builder.Services.Configure<ForwardedHeadersOptions>(
    options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto |
            ForwardedHeaders.XForwardedHost;

        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

var app = builder.Build();
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
