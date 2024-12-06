using Serializer;

var builder = WebApplication.CreateBuilder(args);

builder.Services
	.AddSingleton<IListSerializer, ListSerializer>();

var app = builder.Build();

app.Run();