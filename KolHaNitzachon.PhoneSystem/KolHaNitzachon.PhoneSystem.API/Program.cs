//using Application.Interfaces.External;
using Infrastructure.External;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.External;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.Payment;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.Recordings;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.Repositories;
using KolHaNitzachon.PhoneSystem.Infrastructure.Payment;
using KolHaNitzachon.PhoneSystem.Infrastructure.Persistence;
using KolHaNitzachon.PhoneSystem.Infrastructure.Repositories;
using KolHaNitzachon.PhoneSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
//using KolHaNitzachon.PhoneSystem.Infrastructure.External;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDirectoryBrowser();

builder.Services.AddDbContext<PhoneSystemDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<IPaymentGatewayService, SolaPaymentGatewayService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IRecipientRepository, RecipientRepository>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRecordingStorage, LocalRecordingStorage>();

var app = builder.Build();

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
