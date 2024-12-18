using MqttWebApi;
using MqttWebApi.Service;

var builder = WebApplication.CreateBuilder(args);

// Register MqttService and MqttService1 as scoped services
builder.Services.AddScoped<MqttService>();
builder.Services.AddScoped<AairosService>();

// Add services to the container.
builder.Services.AddSingleton<MqttService>();
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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
