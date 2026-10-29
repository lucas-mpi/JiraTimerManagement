using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace JiraTimerManagement
{
    public class JiraUserConfig
    {
        public string BaseUrl { get; set; } = "https://larcoop.atlassian.net";
        public string Email { get; set; } = "pires.sistemas@lar.ind.br";
        public string ApiToken { get; set; } = "";
        public string SelectedProjectKey { get; set; } = "ALL";
        public bool UseSimulationMode { get; set; } = true;
    }

    public class JiraProjectItem
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Key) || Key == "ALL"
                ? "Todos os projetos atribuídos a mim"
                : $"{Key} — {Name}";
        }
    }

    public class JiraTaskItem
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public string Summary { get; set; }
        public string Status { get; set; }
        public string ProjectKey { get; set; }
        public string ProjectName { get; set; }
        public string Assignee { get; set; }
        public int TimeSpentSeconds { get; set; }

        public string FormattedTimeSpent => JiraService.FormatDuration(TimeSpentSeconds);
    }

    public class SyncWorklogResult
    {
        public bool Success { get; set; }
        public string IssueKey { get; set; }
        public int RawElapsedSeconds { get; set; }
        public int SyncedSeconds { get; set; }
        public string Message { get; set; }
    }

    public class JiraService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();
        private readonly string _configPath;

        // Tarefas simuladas para modo protótipo/demonstração imediata
        private readonly List<JiraTaskItem> _simulatedTasks;

        static JiraService()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public JiraService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "JiraTimerManagement");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            _configPath = Path.Combine(dir, "config.json");

            _simulatedTasks = new List<JiraTaskItem>
            {
                new JiraTaskItem
                {
                    Id = "1001",
                    Key = "NDSTRS-142",
                    Summary = "Otimizar endpoint de integração de notas fiscais de cooperados",
                    Status = "Em Andamento",
                    ProjectKey = "NDSTRS",
                    ProjectName = "Sistemas Industriais",
                    Assignee = "Lucas Mariani Pires",
                    TimeSpentSeconds = 5400 // 1h 30m
                },
                new JiraTaskItem
                {
                    Id = "1002",
                    Key = "NDSTRS-148",
                    Summary = "[Urgente/Ligação] Corrigir travamento na fila de pesagem da balança",
                    Status = "Em Andamento",
                    ProjectKey = "NDSTRS",
                    ProjectName = "Sistemas Industriais",
                    Assignee = "Lucas Mariani Pires",
                    TimeSpentSeconds = 1800 // 30m
                },
                new JiraTaskItem
                {
                    Id = "1003",
                    Key = "NDSTRS-151",
                    Summary = "Criar relatório de apontamento de produção por turno",
                    Status = "A Fazer",
                    ProjectKey = "NDSTRS",
                    ProjectName = "Sistemas Industriais",
                    Assignee = "Lucas Mariani Pires",
                    TimeSpentSeconds = 0
                },
                new JiraTaskItem
                {
                    Id = "1004",
                    Key = "SUP-89",
                    Summary = "[Suporte Ligação] Desbloquear lote de expedição travado no WMS",
                    Status = "Em Atendimento",
                    ProjectKey = "SUP",
                    ProjectName = "Suporte Interno TI",
                    Assignee = "Lucas Mariani Pires",
                    TimeSpentSeconds = 900 // 15m
                },
                new JiraTaskItem
                {
                    Id = "1005",
                    Key = "COOP-310",
                    Summary = "Refatorar autenticação SSO dos portais internos da cooperativa",
                    Status = "Em Revisão",
                    ProjectKey = "COOP",
                    ProjectName = "Portal Cooperativista",
                    Assignee = "Lucas Mariani Pires",
                    TimeSpentSeconds = 7200 // 2h
                }
            };
        }

        public JiraUserConfig LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath, Encoding.UTF8);
                    var cfg = _serializer.Deserialize<JiraUserConfig>(json);
                    if (cfg != null) return cfg;
                }
            }
            catch
            {
                // Ignora erro de leitura e retorna padrão
            }
            return new JiraUserConfig();
        }

        public void SaveConfig(JiraUserConfig config)
        {
            try
            {
                string json = _serializer.Serialize(config);
                File.WriteAllText(_configPath, json, Encoding.UTF8);
            }
            catch
            {
                // Ignora falha de permissão local
            }
        }

        private HttpRequestMessage CreateJiraRequest(HttpMethod method, string relativeUrl, JiraUserConfig config, string jsonBody = null)
        {
            string baseUrl = (config.BaseUrl ?? "https://larcoop.atlassian.net").TrimEnd('/');
            var request = new HttpRequestMessage(method, baseUrl + relativeUrl);

            string rawAuth = $"{config.Email}:{config.ApiToken}";
            string base64Auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(rawAuth));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (jsonBody != null)
            {
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }

            return request;
        }

        public async Task<(bool Success, string DisplayName, string ErrorMessage)> ValidateLoginAsync(JiraUserConfig config)
        {
            if (config.UseSimulationMode)
            {
                await Task.Delay(150);
                return (true, "Lucas Mariani Pires (Modo Protótipo)", null);
            }

            if (string.IsNullOrWhiteSpace(config.Email) || string.IsNullOrWhiteSpace(config.ApiToken))
            {
                return (false, null, "Informe seu e-mail e API Token do Jira.");
            }

            try
            {
                using (var req = CreateJiraRequest(HttpMethod.Get, "/rest/api/3/myself", config))
                using (var resp = await _httpClient.SendAsync(req))
                {
                    string body = await resp.Content.ReadAsStringAsync();
                    if (!resp.IsSuccessStatusCode)
                    {
                        return (false, null, $"Erro Jira ({(int)resp.StatusCode}): Verifique seu e-mail e API Token.");
                    }

                    var dict = _serializer.Deserialize<Dictionary<string, object>>(body);
                    string displayName = dict != null && dict.ContainsKey("displayName")
                        ? Convert.ToString(dict["displayName"])
                        : config.Email;

                    return (true, displayName, null);
                }
            }
            catch (Exception ex)
            {
                return (false, null, $"Falha de conexão: {ex.Message}");
            }
        }

        public async Task<List<JiraProjectItem>> GetProjectsAsync(JiraUserConfig config)
        {
            var list = new List<JiraProjectItem>
            {
                new JiraProjectItem { Key = "ALL", Name = "Todos os projetos atribuídos a mim" }
            };

            if (config.UseSimulationMode)
            {
                await Task.Delay(100);
                list.Add(new JiraProjectItem { Key = "NDSTRS", Name = "Sistemas Industriais" });
                list.Add(new JiraProjectItem { Key = "SUP", Name = "Suporte Interno TI" });
                list.Add(new JiraProjectItem { Key = "COOP", Name = "Portal Cooperativista" });
                return list;
            }

            try
            {
                using (var req = CreateJiraRequest(HttpMethod.Get, "/rest/api/3/project/search?maxResults=100&orderBy=name", config))
                using (var resp = await _httpClient.SendAsync(req))
                {
                    if (!resp.IsSuccessStatusCode) return list;

                    string body = await resp.Content.ReadAsStringAsync();
                    var root = _serializer.Deserialize<Dictionary<string, object>>(body);
                    if (root != null && root.ContainsKey("values") && root["values"] is IEnumerable values)
                    {
                        foreach (var itemObj in values)
                        {
                            if (itemObj is IDictionary<string, object> item)
                            {
                                string key = item.ContainsKey("key") ? Convert.ToString(item["key"]) : "";
                                string name = item.ContainsKey("name") ? Convert.ToString(item["name"]) : key;
                                if (!string.IsNullOrEmpty(key))
                                {
                                    list.Add(new JiraProjectItem { Key = key, Name = name });
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Em caso de falha na listagem de projetos, mantém pelo menos "ALL" e "NDSTRS"
                if (!list.Any(p => p.Key == "NDSTRS"))
                {
                    list.Add(new JiraProjectItem { Key = "NDSTRS", Name = "Projeto NDSTRS" });
                }
            }

            return list;
        }

        public async Task<(List<JiraTaskItem> Tasks, string Error)> GetMyTasksAsync(JiraUserConfig config, string projectKey)
        {
            if (config.UseSimulationMode)
            {
                await Task.Delay(150);
                var query = _simulatedTasks.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "ALL")
                {
                    query = query.Where(t => string.Equals(t.ProjectKey, projectKey, StringComparison.OrdinalIgnoreCase));
                }
                return (query.ToList(), null);
            }

            try
            {
                var jqlParts = new List<string> { "assignee = currentUser()" };
                if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "ALL")
                {
                    jqlParts.Add($"project = \"{projectKey}\"");
                }

                string jql = string.Join(" AND ", jqlParts) + " ORDER BY updated DESC";
                var payload = new Dictionary<string, object>
                {
                    ["jql"] = jql,
                    ["maxResults"] = 40,
                    ["fields"] = new[] { "summary", "status", "timetracking", "assignee", "project" }
                };

                string payloadJson = _serializer.Serialize(payload);
                using (var req = CreateJiraRequest(HttpMethod.Post, "/rest/api/3/search/jql", config, payloadJson))
                using (var resp = await _httpClient.SendAsync(req))
                {
                    string body = await resp.Content.ReadAsStringAsync();
                    if (!resp.IsSuccessStatusCode)
                    {
                        return (new List<JiraTaskItem>(), $"Erro ao consultar Jira ({(int)resp.StatusCode}).");
                    }

                    var tasks = new List<JiraTaskItem>();
                    var root = _serializer.Deserialize<Dictionary<string, object>>(body);
                    if (root != null && root.ContainsKey("issues") && root["issues"] is IEnumerable issues)
                    {
                        foreach (var issueObj in issues)
                        {
                            if (!(issueObj is IDictionary<string, object> issue)) continue;

                            string id = issue.ContainsKey("id") ? Convert.ToString(issue["id"]) : "";
                            string key = issue.ContainsKey("key") ? Convert.ToString(issue["key"]) : "";
                            var fields = issue.ContainsKey("fields") ? issue["fields"] as IDictionary<string, object> : null;

                            string summary = "(Sem título)";
                            string status = "Aberta";
                            string projKey = "";
                            string projName = "";
                            string assignee = "";
                            int timeSpentSeconds = 0;

                            if (fields != null)
                            {
                                if (fields.ContainsKey("summary") && fields["summary"] != null)
                                    summary = Convert.ToString(fields["summary"]);

                                if (fields.ContainsKey("status") && fields["status"] is IDictionary<string, object> st && st.ContainsKey("name"))
                                    status = Convert.ToString(st["name"]);

                                if (fields.ContainsKey("project") && fields["project"] is IDictionary<string, object> prj)
                                {
                                    if (prj.ContainsKey("key")) projKey = Convert.ToString(prj["key"]);
                                    if (prj.ContainsKey("name")) projName = Convert.ToString(prj["name"]);
                                }

                                if (fields.ContainsKey("assignee") && fields["assignee"] is IDictionary<string, object> asg && asg.ContainsKey("displayName"))
                                    assignee = Convert.ToString(asg["displayName"]);

                                if (fields.ContainsKey("timetracking") && fields["timetracking"] is IDictionary<string, object> tt && tt.ContainsKey("timeSpentSeconds") && tt["timeSpentSeconds"] != null)
                                {
                                    int.TryParse(Convert.ToString(tt["timeSpentSeconds"]), out timeSpentSeconds);
                                }
                            }

                            tasks.Add(new JiraTaskItem
                            {
                                Id = id,
                                Key = key,
                                Summary = summary,
                                Status = status,
                                ProjectKey = projKey,
                                ProjectName = projName,
                                Assignee = assignee,
                                TimeSpentSeconds = timeSpentSeconds
                            });
                        }
                    }

                    return (tasks, null);
                }
            }
            catch (Exception ex)
            {
                return (new List<JiraTaskItem>(), $"Falha ao buscar tarefas: {ex.Message}");
            }
        }

        public async Task<SyncWorklogResult> LogWorkAsync(JiraUserConfig config, string issueKey, int elapsedSeconds, string reasonNote = null)
        {
            // Regra definida: Arredondar para 60s (1 min) quando o tempo contado for menor que 1 minuto
            int syncedSeconds = Math.Max(60, elapsedSeconds);
            bool rounded = elapsedSeconds < 60;

            if (config.UseSimulationMode)
            {
                await Task.Delay(250);
                var task = _simulatedTasks.FirstOrDefault(t => t.Key == issueKey);
                if (task != null)
                {
                    task.TimeSpentSeconds += syncedSeconds;
                }

                return new SyncWorklogResult
                {
                    Success = true,
                    IssueKey = issueKey,
                    RawElapsedSeconds = elapsedSeconds,
                    SyncedSeconds = syncedSeconds,
                    Message = rounded
                        ? $"[{issueKey}] +{ FormatDuration(syncedSeconds) } sincronizado no Jira (arredondado de {elapsedSeconds}s para 1m)."
                        : $"[{issueKey}] +{ FormatDuration(syncedSeconds) } sincronizado com sucesso no Jira."
                };
            }

            try
            {
                string commentText = string.IsNullOrWhiteSpace(reasonNote)
                    ? $"Apontamento via Jira Timer Desktop ({FormatDuration(syncedSeconds)})"
                    : reasonNote;

                var payload = new Dictionary<string, object>
                {
                    ["timeSpentSeconds"] = syncedSeconds,
                    ["comment"] = new Dictionary<string, object>
                    {
                        ["type"] = "doc",
                        ["version"] = 1,
                        ["content"] = new object[]
                        {
                            new Dictionary<string, object>
                            {
                                ["type"] = "paragraph",
                                ["content"] = new object[]
                                {
                                    new Dictionary<string, object>
                                    {
                                        ["type"] = "text",
                                        ["text"] = commentText
                                    }
                                }
                            }
                        }
                    }
                };

                string payloadJson = _serializer.Serialize(payload);
                string url = $"/rest/api/3/issue/{Uri.EscapeDataString(issueKey)}/worklog";

                using (var req = CreateJiraRequest(HttpMethod.Post, url, config, payloadJson))
                using (var resp = await _httpClient.SendAsync(req))
                {
                    if (!resp.IsSuccessStatusCode)
                    {
                        string errBody = await resp.Content.ReadAsStringAsync();
                        return new SyncWorklogResult
                        {
                            Success = false,
                            IssueKey = issueKey,
                            RawElapsedSeconds = elapsedSeconds,
                            SyncedSeconds = 0,
                            Message = $"Erro ao sincronizar {issueKey} no Jira ({(int)resp.StatusCode})."
                        };
                    }

                    return new SyncWorklogResult
                    {
                        Success = true,
                        IssueKey = issueKey,
                        RawElapsedSeconds = elapsedSeconds,
                        SyncedSeconds = syncedSeconds,
                        Message = rounded
                            ? $"[{issueKey}] +{FormatDuration(syncedSeconds)} sincronizado no Jira (arredondado de {elapsedSeconds}s)."
                            : $"[{issueKey}] +{FormatDuration(syncedSeconds)} sincronizado no Jira!"
                    };
                }
            }
            catch (Exception ex)
            {
                return new SyncWorklogResult
                {
                    Success = false,
                    IssueKey = issueKey,
                    RawElapsedSeconds = elapsedSeconds,
                    SyncedSeconds = 0,
                    Message = $"Falha de rede ao sincronizar {issueKey}: {ex.Message}"
                };
            }
        }

        public static string FormatDuration(int totalSeconds)
        {
            if (totalSeconds <= 0) return "0m";
            var ts = TimeSpan.FromSeconds(totalSeconds);
            if (ts.TotalHours >= 1)
            {
                return ts.Minutes > 0
                    ? $"{(int)ts.TotalHours}h {ts.Minutes}m"
                    : $"{(int)ts.TotalHours}h";
            }
            if (ts.Minutes >= 1)
            {
                return ts.Seconds > 0
                    ? $"{ts.Minutes}m {ts.Seconds}s"
                    : $"{ts.Minutes}m";
            }
            return $"{ts.Seconds}s";
        }

        public static string FormatDigitalClock(int totalSeconds)
        {
            var ts = TimeSpan.FromSeconds(Math.Max(0, totalSeconds));
            return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }
}
