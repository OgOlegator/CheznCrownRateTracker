using ExchangeRateTracker.Api.Data;
using ExchangeRateTracker.Api.Services;
using ExchangeRateTracker.Api.Services.IServices;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options
    => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<IBankApiService, BankApiService>();

builder.Services.AddTransient<IBankApiService, BankApiService>();
builder.Services.AddTransient<ISynchronizeRatesService, SynchronizeRatesService>();
builder.Services.AddTransient<IReportService, ReportService>();
builder.Services.AddTransient<ISettingsAutoSynhronizeService, SettingsAutoSynhronizeService>();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//��� ��������� ������ �������� ����������� ������� ����� ���� �� http
//app.UseHttpsRedirection();

app.MapControllers();

app.Run();
