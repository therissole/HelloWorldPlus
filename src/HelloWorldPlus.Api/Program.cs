using System.Diagnostics;
using FluentValidation;
using HelloWorldPlus.Api.Features.Authentication;
using HelloWorldPlus.Api.Features.Greetings;
using HelloWorldPlus.Api.Features.Health;
using HelloWorldPlus.Api.Features.Languages;
using HelloWorldPlus.Api.Infrastructure.Authentication;
using HelloWorldPlus.Api.Infrastructure.Authorization;
using HelloWorldPlus.Api.Infrastructure.Identifiers;
using HelloWorldPlus.Api.Infrastructure.Testing;
using HelloWorldPlus.ServiceDefaults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi(options =>
{
    options.CreateSchemaReferenceId = jsonTypeInfo =>
    {
        var defaultId = OpenApiOptions.CreateDefaultSchemaReferenceId(jsonTypeInfo);
        var declaringType = jsonTypeInfo.Type.DeclaringType;

        return defaultId is null || declaringType is null
            ? defaultId
            : $"{declaringType.Name}{defaultId}";
    };
    options.AddBearerSecurity();
    options.AddSchemaTransformer((schema, context, _) =>
    {
        var schemaType = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type)
            ?? context.JsonTypeInfo.Type;
        if (schemaType == typeof(LanguageId)
            || schemaType == typeof(GreetingId))
        {
            schema.Type = JsonSchemaType.String;
            schema.Format = "uuid";
        }

        return Task.CompletedTask;
    });
    options.AddOperationTransformer((operation, _, _) =>
    {
        foreach (var parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
        {
            if (parameter.In != ParameterLocation.Query
                || string.IsNullOrEmpty(parameter.Name))
            {
                continue;
            }

            parameter.Name = char.ToLowerInvariant(parameter.Name[0])
                + parameter.Name[1..];

            if (parameter.Name == "languageId"
                && parameter.Schema is OpenApiSchema schema)
            {
                schema.Type = JsonSchemaType.String;
                schema.Format = "uuid";
            }
        }

        return Task.CompletedTask;
    });
});
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.TraceId.ToString()
            ?? context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddApplicationAuthentication(builder.Configuration);
var entryAssemblyName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
var isOpenApiGeneration = entryAssemblyName is "dotnet-getdocument" or "GetDocument.Insider";
builder.Services.AddOpenFgaAuthorization(enableRecoveryWorker: !isOpenApiGeneration);

var enableClockControl = builder.Configuration.GetValue<bool>("Testing:EnableClockControl");
if (enableClockControl && builder.Environment.IsProduction())
{
    throw new InvalidOperationException(
        "Test-environment clock control cannot be enabled in Production.");
}

if (enableClockControl)
{
    builder.Services.AddSingleton(new ControlledTimeProvider(TimeProvider.System));
    builder.Services.AddSingleton<TimeProvider>(serviceProvider =>
        serviceProvider.GetRequiredService<ControlledTimeProvider>());
}
else
{
    builder.Services.AddSingleton(TimeProvider.System);
}

builder.AddNpgsqlDataSource("DefaultConnection");
builder.Services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);
EntityIdTypeHandlers.Register();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();

var api = app.MapGroup("/api")
    .RequireAuthorization();

api.MapGroup("/health")
    .AllowAnonymous()
    .MapHealthEndpoints();

api.MapGroup("/authentication")
    .AllowAnonymous()
    .MapAuthenticationEndpoints();

api.MapGroup("/languages")
    .MapLanguageEndpoints();

api.MapGroup("/greetings")
    .MapGreetingEndpoints();

if (enableClockControl)
{
    api.MapGroup("/testing")
        .MapTestClockEndpoints();
}

app.Run();

public partial class Program { }
