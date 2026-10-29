using JiraTimerApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient<JiraClientService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors();
app.UseHttpsRedirection();

JiraCredentials ResolveCredentials(HttpContext ctx, IConfiguration config)
{
    var baseUrl = ctx.Request.Headers["X-Jira-Url"].FirstOrDefault()
                  ?? config["Jira:BaseUrl"]
                  ?? "https://larcoop.atlassian.net";

    var email = ctx.Request.Headers["X-Jira-Email"].FirstOrDefault()
                ?? config["Jira:Email"]
                ?? "pires.sistemas@lar.ind.br";

    var token = ctx.Request.Headers["X-Jira-Token"].FirstOrDefault()
                ?? config["Jira:ApiToken"]
                ?? "";

    return new JiraCredentials(baseUrl, email, token);
}

// 1. Valida o login do usuário no Jira e retorna os dados da conta
app.MapGet("/api/me", async (HttpContext ctx, IConfiguration config, JiraClientService jira) =>
{
    var creds = ResolveCredentials(ctx, config);
    var (user, error, statusCode) = await jira.GetCurrentUserAsync(creds);
    if (user == null)
    {
        return Results.Content(error ?? "{\"error\":\"Falha ao autenticar no Jira\"}", "application/json", statusCode: statusCode);
    }
    return Results.Ok(user);
});

// 2. Lista os projetos visíveis para o usuário autenticado (para seleção de time/projeto)
app.MapGet("/api/projetos", async (HttpContext ctx, IConfiguration config, JiraClientService jira) =>
{
    var creds = ResolveCredentials(ctx, config);
    var (projects, error, statusCode) = await jira.GetProjectsAsync(creds);
    if (projects == null)
    {
        return Results.Content(error ?? "{\"error\":\"Falha ao buscar projetos\"}", "application/json", statusCode: statusCode);
    }
    return Results.Ok(projects);
});

// 3. Lista as tarefas atribuídas ao usuário logado (filtrando opcionalmente por projeto)
app.MapGet("/api/tarefas", async (string? projectKey, bool? onlyMine, HttpContext ctx, IConfiguration config, JiraClientService jira) =>
{
    var creds = ResolveCredentials(ctx, config);
    var (issues, error, statusCode) = await jira.GetAssignedIssuesAsync(creds, projectKey, onlyMine ?? true);
    if (issues == null)
    {
        return Results.Content(error ?? "{\"error\":\"Falha ao buscar tarefas\"}", "application/json", statusCode: statusCode);
    }
    return Results.Ok(issues);
});

// 4. Lista cards de Cerimônias (Daily, Planning, Reunião, Chamadas) mesmo sem assignee
app.MapGet("/api/cerimonias", async (string? projectKey, string? query, HttpContext ctx, IConfiguration config, JiraClientService jira) =>
{
    var creds = ResolveCredentials(ctx, config);
    var (issues, error, statusCode) = await jira.GetCeremonyIssuesAsync(creds, projectKey, query);
    if (issues == null)
    {
        return Results.Content(error ?? "{\"error\":\"Falha ao buscar cards de cerimônias\"}", "application/json", statusCode: statusCode);
    }
    return Results.Ok(issues);
});

// 4.1 Lista todos os cards ativos do Scrum e da Sustentação (mesmo sem assignee)
app.MapGet("/api/ativos", async (string? projectKey, HttpContext ctx, IConfiguration config, JiraClientService jira) =>
{
    var creds = ResolveCredentials(ctx, config);
    var (issues, error, statusCode) = await jira.GetAssignedIssuesAsync(creds, projectKey, onlyAssignedToMe: false);
    if (issues == null)
    {
        return Results.Content(error ?? "{\"error\":\"Falha ao buscar cards ativos\"}", "application/json", statusCode: statusCode);
    }
    return Results.Ok(issues);
});

// Endpoint original mantido e corrigido
app.MapGet("/tarefas-jira", async (string? projectKey, HttpContext ctx, IConfiguration config, JiraClientService jira) =>
{
    var creds = ResolveCredentials(ctx, config);
    var targetProject = projectKey ?? config["Jira:DefaultProject"] ?? "NDSTRS";
    var (issues, error, statusCode) = await jira.GetAssignedIssuesAsync(creds, targetProject, onlyAssignedToMe: true);
    if (issues == null)
    {
        return Results.Content(error ?? "{\"error\":\"Falha ao consultar Jira\"}", "application/json", statusCode: statusCode);
    }
    return Results.Ok(issues);
});

// 5. Registra horas (Worklog cronometrado ou manual) + comentário na tarefa do Jira
app.MapPost("/api/tarefas/{issueKey}/worklog", async (
    string issueKey,
    WorklogRequestDto request,
    HttpContext ctx,
    IConfiguration config,
    JiraClientService jira) =>
{
    var creds = ResolveCredentials(ctx, config);
    var (result, error, statusCode) = await jira.AddWorklogAsync(
        creds,
        issueKey,
        request.TimeSpentSeconds,
        request.TimeSpentText,
        request.Comment);

    if (result == null)
    {
        return Results.Content(error ?? "{\"error\":\"Falha ao registrar worklog no Jira\"}", "application/json", statusCode: statusCode);
    }
    return Results.Ok(result);
});

app.Run();
