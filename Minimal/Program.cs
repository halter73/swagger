using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMethodInfoApiExplorerServices();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MinimalSample", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Minimal v1"));
}

Func<string> helloWorld = () => "Hello World!";
app.MapGet("/text", helloWorld);//.Add(endpointBuilder => endpointBuilder.Metadata.Add(helloWorld.Method));

Func<HelloRecord> jsonHello = () => new("Hello World!");
app.MapGet("/json", jsonHello).Add(endpointBuilder => endpointBuilder.Metadata.Add(jsonHello.Method));

//app.MapGet("/", (Func<string>)(() => "Hello World!"));

app.Run();

record HelloRecord(string message);
