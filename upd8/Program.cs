using upd8.Options;
using upd8.Services.Hardware;
using upd8.Services.Info;
using upd8.Services.Software;
using upd8.Services.Updates;
using Velopack;

var builder = WebApplication.CreateBuilder(args);

VelopackApp.Build().Run();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<UpdateSettings>(builder.Configuration.GetSection(UpdateSettings.SectionName));

builder.Services.AddSingleton<IHardwareService, WmiHardwareService>();
builder.Services.AddSingleton<IInfoService, SystemInfoService>();
builder.Services.AddSingleton<ISoftwareService, RegistrySoftwareService>();
builder.Services.AddSingleton<IUpdateService, VelopackUpdateService>();
builder.Services.AddHostedService<UpdateStartupService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
