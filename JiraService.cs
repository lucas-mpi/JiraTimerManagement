using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
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
        public List<string> PinnedIssueKeys { get; set; } = new List<string> { "NDSTRS-100", "NDSTRS-101", "SUP-10" };
    }

    public class JiraProjectItem
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Key) || Key == "ALL"
                ? "Todos os projetos"
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
        public bool IsAssignedToMe { get; set; } = true;
        public bool IsCeremonyOrShared { get; set; } = false;
        public string LastComment { get; set; } = "";

        public string FormattedTimeSpent => JiraService.FormatDuration(TimeSpentSeconds);
    }

    public class SyncWorklogResult
    {
        public bool Success { get; set; }
        public string IssueKey { get; set; }
        public int RawElapsedSeconds { get; set; }
        public int SyncedSeconds { get; set; }
        public string Comment { get; set; }
        public string Message { get; set; }
    }

    public class JiraService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();
        private readonly string _configPath;

        // Base simulada contendo tanto tarefas atribuídas ao dev quanto cards de Cerimônias/Dailys não atribuídos
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
                // Tarefas atribuídas ao desenvolvedor
                new JiraTaskItem
                {
                    Id = "1001",
                    Key = "NDSTRS-142",
                    Summary = "Otimizar endpoint de integração de notas fiscais de cooperados",
                    Status = "Em Andamento",
                    ProjectKey = "NDSTRS",
                    ProjectName = "Sistemas Industriais",
                    Assignee = "Lucas Mariani Pires",
                    TimeSpentSeconds = 5400, // 1h 30m
                    IsAssignedToMe = true,
                    IsCeremonyOrShared = false
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
                    TimeSpentSeconds = 1800, // 30m
                    IsAssignedToMe = true,
                    IsCeremonyOrShared = false
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
                    TimeSpentSeconds = 0,
                    IsAssignedToMe = true,
                    IsCeremonyOrShared = false
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
                    TimeSpentSeconds = 900, // 15m
                    IsAssignedToMe = true,
                    IsCeremonyOrShared = false
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
                    TimeSpentSeconds = 7200, // 2h
                    IsAssignedToMe = true,
                    IsCeremonyOrShared = false
                },

                // Cards de Cerimônias / Dailys / Chamadas (Não atribuídos a um dev específico)
                new JiraTaskItem
                {
                    Id = "2001",
                    Key = "NDSTRS-100",
                    Summary = "[Cerimônia] Daily Scrum & Alinhamentos Diários da Equipe",
                    Status = "Recorrente Sprint",
                    ProjectKey = "NDSTRS",
                    ProjectName = "Sistemas Industriais",
                    Assignee = "Não atribuído (Cerimônia)",
                    TimeSpentSeconds = 10800, // 3h acumuladas no time
                    IsAssignedToMe = false,
                    IsCeremonyOrShared = true
                },
                new JiraTaskItem
                {
                    Id = "2002",
                    Key = "NDSTRS-101",
                    Summary = "[Cerimônia] Planning, Refinamento Técnico & Review da Sprint",
                    Status = "Recorrente Sprint",
                    ProjectKey = "NDSTRS",
                    ProjectName = "Sistemas Industriais",
                    Assignee = "Não atribuído (Cerimônia)",
                    TimeSpentSeconds = 7200, // 2h
                    IsAssignedToMe = false,
                    IsCeremonyOrShared = true
                },
                new JiraTaskItem
                {
                    Id = "2003",
                    Key = "SUP-10",
                    Summary = "[Cerimônia / Chamadas] Ligações Rápidas, Alinhamentos e Plantão TI",
                    Status = "Recorrente",
                    ProjectKey = "SUP",
                    ProjectName = "Suporte Interno TI",
                    Assignee = "Não atribuído (Cerimônia)",
                    TimeSpentSeconds = 3600, // 1h
                    IsAssignedToMe = false,
                    IsCeremonyOrShared = true
                },
                new JiraTaskItem
                {
                    Id = "2004",
                    Key = "COOP-50",
                    Summary = "[Cerimônia] Reunião Geral de Arquitetura e Sincronização de Times",
                    Status = "Recorrente",
                    ProjectKey = "COOP",
                    ProjectName = "Portal Cooperativista",
                    Assignee = "Não atribuído (Cerimônia)",
                    TimeSpentSeconds = 1800, // 30m
                    IsAssignedToMe = false,
                    IsCeremonyOrShared = true
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
                    if (cfg != null)
                    {
                        if (cfg.PinnedIssueKeys == null)
                            cfg.PinnedIssueKeys = new List<string> { "NDSTRS-100", "NDSTRS-101", "SUP-10" };
                        return cfg;
                    }
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
                await Task.Delay(120);
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
                new JiraProjectItem { Key = "ALL", Name = "Todos os projetos" }
            };

            if (config.UseSimulationMode)
            {
                await Task.Delay(80);
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
                if (!list.Any(p => p.Key == "NDSTRS"))
                {
                    list.Add(new JiraProjectItem { Key = "NDSTRS", Name = "Projeto NDSTRS" });
                }
            }

            return list;
        }

        public async Task<(List<JiraTaskItem> Tasks, string Error)> GetTasksForTabAsync(
            JiraUserConfig config,
            string projectKey,
            bool ceremoniesTab)
        {
            var pinnedSet = new HashSet<string>(
                config.PinnedIssueKeys ?? new List<string>(),
                StringComparer.OrdinalIgnoreCase);

            if (config.UseSimulationMode)
            {
                await Task.Delay(120);
                IEnumerable<JiraTaskItem> query;

                if (!ceremoniesTab)
                {
                    query = _simulatedTasks.Where(t => t.IsAssignedToMe && !IsDoneStatus(t.Status));
                }
                else
                {
                    query = _simulatedTasks.Where(t => (t.IsCeremonyOrShared || pinnedSet.Contains(t.Key)) && !IsDoneStatus(t.Status));
                }

                if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "ALL")
                {
                    query = query.Where(t => string.Equals(t.ProjectKey, projectKey, StringComparison.OrdinalIgnoreCase));
                }

                // Ordena colocando os fixados (⭐) primeiro
                var ordered = query
                    .OrderByDescending(t => pinnedSet.Contains(t.Key))
                    .ThenBy(t => t.Key)
                    .ToList();

                return (ordered, null);
            }

            try
            {
                string jql;
                if (!ceremoniesTab)
                {
                    var jqlParts = new List<string>
                    {
                        "assignee = currentUser()",
                        "statusCategory != Done"
                    };
                    if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "ALL")
                    {
                        jqlParts.Add($"project = \"{projectKey}\"");
                    }
                    jql = string.Join(" AND ", jqlParts) + " ORDER BY updated DESC";
                }
                else
                {
                    var orClauses = new List<string>
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

                    var validPinned = pinnedSet
                        .Where(k => Regex.IsMatch(k, @"^[A-Za-z0-9]+-\d+$"))
                        .Select(k => $"\"{k.ToUpperInvariant()}\"")
                        .ToList();

                    if (validPinned.Count > 0)
                    {
                        orClauses.Add($"key IN ({string.Join(",", validPinned)})");
                    }

                    var jqlParts = new List<string>
                    {
                        $"({string.Join(" OR ", orClauses)})",
                        "statusCategory != Done"
                    };
                    if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "ALL")
                    {
                        jqlParts.Add($"project = \"{projectKey}\"");
                    }

                    jql = string.Join(" AND ", jqlParts) + " ORDER BY updated DESC";
                }

                return await ExecuteJqlAsync(config, jql, !ceremoniesTab, pinnedSet);
            }
            catch (Exception ex)
            {
                return (new List<JiraTaskItem>(), $"Falha ao buscar tarefas: {ex.Message}");
            }
        }

        public async Task<(JiraTaskItem Task, string Error)> FindOrCreateCardByKeyAsync(JiraUserConfig config, string issueKey, string customTitle = null)
        {
            if (string.IsNullOrWhiteSpace(issueKey))
                return (null, "Informe o código do card (ex: NDSTRS-100).");

            string cleanKey = issueKey.Trim().ToUpperInvariant();

            if (config.UseSimulationMode)
            {
                await Task.Delay(100);
                var existing = _simulatedTasks.FirstOrDefault(t => string.Equals(t.Key, cleanKey, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    return (existing, null);
                }

                string proj = cleanKey.Contains("-") ? cleanKey.Split('-')[0] : "NDSTRS";
                var created = new JiraTaskItem
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Key = cleanKey,
                    Summary = string.IsNullOrWhiteSpace(customTitle)
                        ? $"[Cerimônia / Card Fixado] Atividade compartilhada ({cleanKey})"
                        : customTitle.Trim(),
                    Status = "Ativo",
                    ProjectKey = proj,
                    ProjectName = proj,
                    Assignee = "Não atribuído (Cerimônia/Equipe)",
                    TimeSpentSeconds = 0,
                    IsAssignedToMe = false,
                    IsCeremonyOrShared = true
                };
                _simulatedTasks.Add(created);
                return (created, null);
            }

            string jql = $"key = \"{cleanKey}\"";
            var (list, err) = await ExecuteJqlAsync(config, jql, false, new HashSet<string>());
            if (!string.IsNullOrEmpty(err)) return (null, err);
            var found = list.FirstOrDefault();
            if (found == null) return (null, $"Card {cleanKey} não encontrado no Jira.");
            return (found, null);
        }

        private async Task<(List<JiraTaskItem> Tasks, string Error)> ExecuteJqlAsync(
            JiraUserConfig config,
            string jql,
            bool isMyTasksTab,
            HashSet<string> pinnedSet)
        {
            var payload = new Dictionary<string, object>
            {
                ["jql"] = jql,
                ["maxResults"] = 45,
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
                        string assignee = "Não atribuído (Cerimônia/Equipe)";
                        bool hasAssignee = false;
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

                            if (fields.ContainsKey("assignee") && fields["assignee"] is IDictionary<string, object> asg && asg.ContainsKey("displayName") && asg["displayName"] != null)
                            {
                                assignee = Convert.ToString(asg["displayName"]);
                                hasAssignee = true;
                            }

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
                            TimeSpentSeconds = timeSpentSeconds,
                            IsAssignedToMe = isMyTasksTab || hasAssignee,
                            IsCeremonyOrShared = !isMyTasksTab || !hasAssignee
                        });
                    }
                }

                tasks = tasks
                    .OrderByDescending(t => pinnedSet != null && pinnedSet.Contains(t.Key))
                    .ToList();

                return (tasks, null);
            }
        }

        public async Task<SyncWorklogResult> LogWorkAsync(JiraUserConfig config, string issueKey, int elapsedSeconds, string commentNote = null)
        {
            // Regra definida: Arredondar para 60s (1 min) quando o tempo contado for menor que 1 minuto
            int syncedSeconds = Math.Max(60, elapsedSeconds);
            bool rounded = elapsedSeconds < 60;

            string finalComment = string.IsNullOrWhiteSpace(commentNote)
                ? $"Apontamento via Jira Focus Timer ({FormatDuration(syncedSeconds)})"
                : commentNote.Trim();

            if (config.UseSimulationMode)
            {
                await Task.Delay(200);
                var task = _simulatedTasks.FirstOrDefault(t => string.Equals(t.Key, issueKey, StringComparison.OrdinalIgnoreCase));
                if (task != null)
                {
                    task.TimeSpentSeconds += syncedSeconds;
                    if (!string.IsNullOrWhiteSpace(commentNote))
                    {
                        task.LastComment = commentNote.Trim();
                    }
                }

                string commentPreview = string.IsNullOrWhiteSpace(commentNote) ? "" : $" 💬 \"{Truncate(commentNote.Trim(), 32)}\"";
                return new SyncWorklogResult
                {
                    Success = true,
                    IssueKey = issueKey,
                    RawElapsedSeconds = elapsedSeconds,
                    SyncedSeconds = syncedSeconds,
                    Comment = finalComment,
                    Message = rounded
                        ? $"[{issueKey}] +{FormatDuration(syncedSeconds)} sincronizado (arredondado de {elapsedSeconds}s para 1m).{commentPreview}"
                        : $"[{issueKey}] +{FormatDuration(syncedSeconds)} sincronizado no Jira!{commentPreview}"
                };
            }

            try
            {
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
                                        ["text"] = finalComment
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
                        return new SyncWorklogResult
                        {
                            Success = false,
                            IssueKey = issueKey,
                            RawElapsedSeconds = elapsedSeconds,
                            SyncedSeconds = 0,
                            Comment = finalComment,
                            Message = $"Erro ao sincronizar {issueKey} no Jira ({(int)resp.StatusCode})."
                        };
                    }

                    string commentPreview = string.IsNullOrWhiteSpace(commentNote) ? "" : $" 💬 \"{Truncate(commentNote.Trim(), 30)}\"";
                    return new SyncWorklogResult
                    {
                        Success = true,
                        IssueKey = issueKey,
                        RawElapsedSeconds = elapsedSeconds,
                        SyncedSeconds = syncedSeconds,
                        Comment = finalComment,
                        Message = rounded
                            ? $"[{issueKey}] +{FormatDuration(syncedSeconds)} sincronizado no Jira (arredondado de {elapsedSeconds}s).{commentPreview}"
                            : $"[{issueKey}] +{FormatDuration(syncedSeconds)} sincronizado no Jira!{commentPreview}"
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
                    Comment = finalComment,
                    Message = $"Falha de rede ao sincronizar {issueKey}: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Converte textos como "15m", "1h 30m", "2h", "45" (minutos) em segundos.
        /// </summary>
        public static int ParseDurationToSeconds(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return 0;
            string text = input.Trim().ToLowerInvariant();

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

        private static bool IsDoneStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;
            string s = status.Trim().ToLowerInvariant();
            return s == "done" || s == "concluído" || s == "concluido" || s == "concluída" || s == "concluida" || s == "closed" || s == "fechado" || s == "finalizado";
        }

        private static string Truncate(string value, int maxLen)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLen) return value;
            return value.Substring(0, maxLen) + "...";
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
