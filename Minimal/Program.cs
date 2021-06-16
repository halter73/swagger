using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
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

//Func<string> helloWorld = () => "Hello World!";
app.MapGet("/text", (int test) => "Hello World!");//.Add(endpointBuilder => endpointBuilder.Metadata.Add(helloWorld.Method));

app.MapGet("/text/{test}", (int? test) => test);

app.MapGet("/json", (Func<HelloRecord>)(() => new("Hello World!")));

app.Map("/many", (Func<string>)(() => "Hello World!"));

app.MapGet("/void", () => { });

app.MapGet("/task", () => Task.CompletedTask);

app.MapPost("/iresult", (HelloRecord hello) =>
{
    return new JsonResult(hello);
});

app.MapPost("/frombody", ([FromBody] int hello) =>
{
    return new JsonResult(hello);
});

app.MapPost("/frombodyextra",
    [ProducesResponseType(typeof(DateTime), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("text/plain")]
    [Consumes("custom/json")]
    ([FromBody] int hello) =>
    {
        return new JsonResult(hello);
    });

app.MapPost("/testclass", TestClass.EchoHelloRecord);

app.Run();

public record HelloRecord(string message);

public static class TestClass
{
    public static HelloRecord EchoHelloRecord(HelloRecord hello)
    {
        return hello;
    }
}