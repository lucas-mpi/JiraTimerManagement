using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace JiraTimerApi;

public record JiraCredentials(string BaseUrl, string Email, string ApiToken);

public record JiraUserDto(string AccountId, string DisplayName, string EmailAddress);

public record JiraProjectDto(string Id, string Key, string Name);

public record JiraIssueDto(
    string Id,
    string Key,
    string Summary,
    string Status,
    string ProjectKey,
    string ProjectName,
    string AssigneeName,
    int TimeSpentSeconds,
    string TimeSpentFormatted,
    bool IsAssignedToCurrentUser
);

public record WorklogRequestDto(int TimeSpentSeconds = 0, string? TimeSpentText = null, string? Comment = null);

public record WorklogResponseDto(
    bool Success,
    string IssueKey,
    int OriginalSeconds,
    int SyncedSeconds,
    string SyncedFormatted,
    string CommentUsed,
    string Message
);

public class JiraClientService
{
    private readonly HttpClient _httpClient;

    public JiraClientService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativeUrl, JiraCredentials creds, HttpContent? content = null)
    {
        var baseUrl = creds.BaseUrl.TrimEnd('/');
        var request = new HttpRequestMessage(method, $"{baseUrl}{relativeUrl}");

        var authRaw = $"{creds.Email}:{creds.ApiToken}";
        var authBase64 = Convert.ToBase64String(Encoding.ASCII.GetBytes(authRaw));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authBase64);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (content != null)
        {
            request.Content = content;
        }

        return request;
    }

    public async Task<(JiraUserDto? User, string? Error, int StatusCode)> GetCurrentUserAsync(JiraCredentials creds)
    {
        using var request = CreateRequest(HttpMethod.Get, "/rest/api/3/myself", creds);
        using var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return (null, body, (int)response.StatusCode);
        }

        var node = JsonNode.Parse(body);
        var user = new JiraUserDto(
            AccountId: node?["accountId"]?.ToString() ?? "",
            DisplayName: node?["displayName"]?.ToString() ?? creds.Email,
            EmailAddress: node?["emailAddress"]?.ToString() ?? creds.Email
        );

        return (user, null, 200);
    }

    public async Task<(List<JiraProjectDto>? Projects, string? Error, int StatusCode)> GetProjectsAsync(JiraCredentials creds)
    {
        using var request = CreateRequest(HttpMethod.Get, "/rest/api/3/project/search?maxResults=100&orderBy=name", creds);
        using var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return (null, body, (int)response.StatusCode);
        }

        var node = JsonNode.Parse(body);
        var values = node?["values"]?.AsArray();
        var result = new List<JiraProjectDto>();

        if (values != null)
        {
            foreach (var item in values)
            {
                if (item == null) continue;
                result.Add(new JiraProjectDto(
                    Id: item["id"]?.ToString() ?? "",
                    Key: item["key"]?.ToString() ?? "",
                    Name: item["name"]?.ToString() ?? ""
                ));
            }
        }

        return (result, null, 200);
    }

    public async Task<(List<JiraIssueDto>? Issues, string? Error, int StatusCode)> GetAssignedIssuesAsync(
        JiraCredentials creds,
        string? projectKey = null,
        bool onlyAssignedToMe = true)
    {
        var jqlParts = new List<string> { "statusCategory != Done" };

        if (onlyAssignedToMe)
        {
            jqlParts.Add("assignee = currentUser()");
        }

        if (!string.IsNullOrWhiteSpace(projectKey) && !projectKey.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            jqlParts.Add($"project = \"{projectKey.Trim()}\"");
        }

        var whereClause = jqlParts.Count > 0 ? string.Join(" AND ", jqlParts) : "created >= -30d";
        var jql = $"{whereClause} ORDER BY updated DESC";

        return await ExecuteJqlSearchAsync(creds, jql, onlyAssignedToMe);
    }

    /// <summary>
    /// Busca cards de Cerimônias (Daily, Planning, Retro, Reunião, Chamadas) ou cards fixados (pinnedKeys),
    /// mesmo que não estejam atribuídos a nenhum desenvolvedor.
    /// </summary>
    public async Task<(List<JiraIssueDto>? Issues, string? Error, int StatusCode)> GetCeremonyAndPinnedIssuesAsync(
        JiraCredentials creds,
        string? projectKey = null,
        string? pinnedKeysCsv = null,
        string? searchText = null)
    {
        var orConditions = new List<string>
        {
            "summary ~ \"Daily\"",
            "summary ~ \"Cerimonia\"",
            "summary ~ \"Cerimônia\"",
            "summary ~ \"Reuniao\"",
            "summary ~ \"Reunião\"",
            "summary ~ \"Planning\"",
            "summary ~ \"Refinamento\"",
            "summary ~ \"Retro\"",
            "summary ~ \"Chamada\""
        };

        if (!string.IsNullOrWhiteSpace(pinnedKeysCsv))
        {
            var keys = pinnedKeysCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(k => Regex.IsMatch(k, @"^[A-Za-z0-9]+-\d+$"))
                .Select(k => $"\"{k.ToUpperInvariant()}\"")
                .ToList();

            if (keys.Count > 0)
            {
                orConditions.Add($"key IN ({string.Join(",", keys)})");
            }
        }

        string filterClause;
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var clean = searchText.Trim().Replace("\"", "");
            if (Regex.IsMatch(clean, @"^[A-Za-z0-9]+-\d+$"))
            {
                filterClause = $"key = \"{clean.ToUpperInvariant()}\"";
            }
            else
            {
                filterClause = $"summary ~ \"{clean}\"";
            }
        }
        else
        {
            filterClause = $"({string.Join(" OR ", orConditions)})";
        }

        var jqlParts = new List<string> { filterClause, "statusCategory != Done" };
        if (!string.IsNullOrWhiteSpace(projectKey) && !projectKey.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            jqlParts.Add($"project = \"{projectKey.Trim()}\"");
        }

        var jql = $"{string.Join(" AND ", jqlParts)} ORDER BY updated DESC";
        return await ExecuteJqlSearchAsync(creds, jql, false);
    }

    private async Task<(List<JiraIssueDto>? Issues, string? Error, int StatusCode)> ExecuteJqlSearchAsync(
        JiraCredentials creds,
        string jql,
        bool isAssignedFilter)
    {
        var payloadObj = new
        {
            jql,
            maxResults = 50,
            fields = new[] { "summary", "status", "timetracking", "assignee", "project" }
        };

        var payloadJson = JsonSerializer.Serialize(payloadObj);
        using var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
        using var request = CreateRequest(HttpMethod.Post, "/rest/api/3/search/jql", creds, content);
        using var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return (null, body, (int)response.StatusCode);
        }

        var root = JsonNode.Parse(body);
        var issuesArray = root?["issues"]?.AsArray();
        var list = new List<JiraIssueDto>();

        if (issuesArray != null)
        {
            foreach (var issue in issuesArray)
            {
                if (issue == null) continue;
                var fields = issue["fields"];
                var timeTracking = fields?["timetracking"];
                int seconds = timeTracking?["timeSpentSeconds"]?.GetValue<int>() ?? 0;
                string formatted = timeTracking?["timeSpent"]?.ToString() ?? FormatSeconds(seconds);
                string? assigneeName = fields?["assignee"]?["displayName"]?.ToString();

                list.Add(new JiraIssueDto(
                    Id: issue["id"]?.ToString() ?? "",
                    Key: issue["key"]?.ToString() ?? "",
                    Summary: fields?["summary"]?.ToString() ?? "(Sem título)",
                    Status: fields?["status"]?["name"]?.ToString() ?? "Aberta",
                    ProjectKey: fields?["project"]?["key"]?.ToString() ?? "",
                    ProjectName: fields?["project"]?["name"]?.ToString() ?? "",
                    AssigneeName: string.IsNullOrWhiteSpace(assigneeName) ? "Não atribuído (Equipe/Cerimônia)" : assigneeName,
                    TimeSpentSeconds: seconds,
                    TimeSpentFormatted: formatted,
                    IsAssignedToCurrentUser: isAssignedFilter || !string.IsNullOrWhiteSpace(assigneeName)
                ));
            }
        }

        return (list, null, 200);
    }

    public async Task<(WorklogResponseDto? Result, string? Error, int StatusCode)> AddWorklogAsync(
        JiraCredentials creds,
        string issueKey,
        int elapsedSeconds,
        string? timeSpentText = null,
        string? commentText = null)
    {
        int rawSeconds = elapsedSeconds;
        if (rawSeconds <= 0 && !string.IsNullOrWhiteSpace(timeSpentText))
        {
            rawSeconds = ParseDurationToSeconds(timeSpentText);
        }

        // Regra definida: arredondar para 60s (1 min) quando o tempo contado for menor que 1 minuto
        int syncedSeconds = Math.Max(60, rawSeconds);
        string finalComment = string.IsNullOrWhiteSpace(commentText)
            ? $"Registro via Jira Focus Timer ({FormatSeconds(syncedSeconds)})"
            : commentText.Trim();

        var worklogPayload = new
        {
            timeSpentSeconds = syncedSeconds,
            comment = new
            {
                type = "doc",
                version = 1,
                content = new[]
                {
                    new
                    {
                        type = "paragraph",
                        content = new[]
                        {
                            new
                            {
                                type = "text",
                                text = finalComment
                            }
                        }
                    }
                }
            }
        };

        var payloadJson = JsonSerializer.Serialize(worklogPayload);
        using var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
        using var request = CreateRequest(HttpMethod.Post, $"/rest/api/3/issue/{Uri.EscapeDataString(issueKey)}/worklog", creds, content);
        using var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return (null, body, (int)response.StatusCode);
        }

        var result = new WorklogResponseDto(
            Success: true,
            IssueKey: issueKey,
            OriginalSeconds: rawSeconds,
            SyncedSeconds: syncedSeconds,
            SyncedFormatted: FormatSeconds(syncedSeconds),
            CommentUsed: finalComment,
            Message: rawSeconds < 60
                ? $"Tempo de {rawSeconds}s arredondado para 1m (60s) e sincronizado na tarefa {issueKey}."
                : $"Tempo de {FormatSeconds(syncedSeconds)} sincronizado com sucesso na tarefa {issueKey}."
        );

        return (result, null, 200);
    }

    public static int ParseDurationToSeconds(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return 0;
        var text = input.Trim().ToLowerInvariant();

        // Caso seja apenas número (ex: "15" -> 15 minutos)
        if (int.TryParse(text, out int plainMinutes))
        {
            return plainMinutes * 60;
        }

        int totalSeconds = 0;
        var matches = Regex.Matches(text, @"(\d+)\s*([hms])");
        foreach (Match m in matches)
        {
            int val = int.Parse(m.Groups[1].Value);
            string unit = m.Groups[2].Value;
            if (unit == "h") totalSeconds += val * 3600;
            else if (unit == "m") totalSeconds += val * 60;
            else if (unit == "s") totalSeconds += val;
        }

        return totalSeconds;
    }

    public static string FormatSeconds(int totalSeconds)
    {
        if (totalSeconds <= 0) return "0m";
        var ts = TimeSpan.FromSeconds(totalSeconds);
        if (ts.TotalHours >= 1)
        {
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        }
        if (ts.Minutes >= 1)
        {
            return ts.Seconds > 0 ? $"{ts.Minutes}m {ts.Seconds}s" : $"{ts.Minutes}m";
        }
        return $"{ts.Seconds}s";
    }
}
