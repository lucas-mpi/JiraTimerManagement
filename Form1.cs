using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JiraTimerManagement
{
    public partial class Form1 : Form
    {
        private readonly JiraService _jiraService = new JiraService();
        private JiraUserConfig _config;
        private List<JiraTaskItem> _allTasks = new List<JiraTaskItem>();

        // Estado do cronômetro ativo
        private JiraTaskItem _activeTask = null;
        private DateTime _activeStartTimeUtc;
        private int _elapsedSeconds = 0;

        // Modo Mini-Widget compacto
        private bool _isMiniMode = false;
        private int _expandedHeight = 680;
        private bool _suppressProjectChange = false;

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            _config = _jiraService.LoadConfig();

            txtBaseUrl.Text = _config.BaseUrl;
            txtEmail.Text = _config.Email;
            txtApiToken.Text = _config.ApiToken;
            chkSimulationMode.Checked = _config.UseSimulationMode;

            UpdateCredentialFieldsState();
            await ConnectAndLoadAllAsync();
        }

        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Se houver uma tarefa rodando ao fechar o aplicativo, pergunta se deseja sincronizar antes de sair
            if (_activeTask != null && _elapsedSeconds > 0)
            {
                var answer = MessageBox.Show(
                    $"A tarefa [{_activeTask.Key}] está com o cronômetro ativo ({JiraService.FormatDigitalClock(_elapsedSeconds)}).\n\nDeseja parar e sincronizar essas horas no Jira antes de fechar?",
                    "Sincronizar horas pendentes?",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (answer == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (answer == DialogResult.Yes)
                {
                    e.Cancel = true;
                    await StopAndSyncActiveTaskAsync("Fechamento do aplicativo");
                    this.Close();
                }
            }
        }

        private void UpdateCredentialFieldsState()
        {
            bool isRealJira = !chkSimulationMode.Checked;
            txtBaseUrl.Enabled = isRealJira;
            txtEmail.Enabled = isRealJira;
            txtApiToken.Enabled = isRealJira;
        }

        private void ChkSimulationMode_CheckedChanged(object sender, EventArgs e)
        {
            UpdateCredentialFieldsState();
        }

        private void ChkTopMost_CheckedChanged(object sender, EventArgs e)
        {
            this.TopMost = chkTopMost.Checked;
            SetStatus(this.TopMost
                ? "📌 Janela fixada sempre no topo (ideal durante ligações/atendimentos)."
                : "Janela em modo normal.", false);
        }

        private void BtnToggleMini_Click(object sender, EventArgs e)
        {
            _isMiniMode = !_isMiniMode;

            if (_isMiniMode)
            {
                _expandedHeight = this.Height;
                pnlConfig.Visible = false;
                pnlFilters.Visible = false;
                flpTasks.Visible = false;
                this.ClientSize = new Size(this.ClientSize.Width, pnlHeader.Height + pnlActiveTimer.Height + pnlFooter.Height);
                btnToggleMini.Text = "🗖 Lista";
                this.TopMost = true;
                chkTopMost.Checked = true;
            }
            else
            {
                pnlFilters.Visible = true;
                flpTasks.Visible = true;
                this.Height = Math.Max(560, _expandedHeight);
                btnToggleMini.Text = "🗕 Mini";
            }
        }

        private void BtnToggleConfig_Click(object sender, EventArgs e)
        {
            if (_isMiniMode)
            {
                BtnToggleMini_Click(sender, e);
            }
            pnlConfig.Visible = !pnlConfig.Visible;
        }

        private async void BtnSaveConnect_Click(object sender, EventArgs e)
        {
            _config.BaseUrl = txtBaseUrl.Text.Trim();
            _config.Email = txtEmail.Text.Trim();
            _config.ApiToken = txtApiToken.Text.Trim();
            _config.UseSimulationMode = chkSimulationMode.Checked;

            _jiraService.SaveConfig(_config);
            pnlConfig.Visible = false;

            await ConnectAndLoadAllAsync();
        }

        private async Task ConnectAndLoadAllAsync()
        {
            SetStatus("Autenticando conta e carregando projetos do Jira...", false);
            lblUserStatus.Text = "Autenticando...";

            var loginRes = await _jiraService.ValidateLoginAsync(_config);
            if (!loginRes.Success)
            {
                lblUserStatus.Text = "⚠ Não autenticado (clique em ⚙ Conta)";
                lblUserStatus.ForeColor = Color.FromArgb(248, 113, 113);
                SetStatus(loginRes.ErrorMessage, true);
                pnlConfig.Visible = true;
                return;
            }

            lblUserStatus.Text = $"👤 {loginRes.DisplayName}";
            lblUserStatus.ForeColor = _config.UseSimulationMode
                ? Color.FromArgb(250, 204, 21)
                : Color.FromArgb(52, 211, 153);

            // Carrega projetos para o seletor de times
            _suppressProjectChange = true;
            var projects = await _jiraService.GetProjectsAsync(_config);
            cmbProjects.Items.Clear();
            int selectedIdx = 0;

            for (int i = 0; i < projects.Count; i++)
            {
                cmbProjects.Items.Add(projects[i]);
                if (string.Equals(projects[i].Key, _config.SelectedProjectKey, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIdx = i;
                }
            }

            if (cmbProjects.Items.Count > 0)
            {
                cmbProjects.SelectedIndex = selectedIdx;
            }
            _suppressProjectChange = false;

            await RefreshTasksAsync();
        }

        private async void CmbProjects_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppressProjectChange) return;

            if (cmbProjects.SelectedItem is JiraProjectItem selectedProj)
            {
                _config.SelectedProjectKey = selectedProj.Key;
                _jiraService.SaveConfig(_config);
                await RefreshTasksAsync();
            }
        }

        private async void BtnRefresh_Click(object sender, EventArgs e)
        {
            await RefreshTasksAsync();
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            RenderTaskCards();
        }

        private void FlpTasks_SizeChanged(object sender, EventArgs e)
        {
            flpTasks.SuspendLayout();
            int cardWidth = Math.Max(360, flpTasks.ClientSize.Width - 26);
            foreach (Control ctrl in flpTasks.Controls)
            {
                ctrl.Width = cardWidth;
            }
            flpTasks.ResumeLayout();
        }

        private async Task RefreshTasksAsync()
        {
            string projectKey = (cmbProjects.SelectedItem as JiraProjectItem)?.Key ?? "ALL";
            btnRefresh.Enabled = false;
            SetStatus($"Buscando tarefas atribuídas a você ({(projectKey == "ALL" ? "Todos os projetos" : projectKey)})...", false);

            var (tasks, error) = await _jiraService.GetMyTasksAsync(_config, projectKey);
            btnRefresh.Enabled = true;

            if (!string.IsNullOrEmpty(error))
            {
                SetStatus(error, true);
                return;
            }

            _allTasks = tasks;
            RenderTaskCards();
            SetStatus($"✓ {_allTasks.Count} tarefa(s) carregada(s). Clique em ▶ Iniciar para contar horas.", false);
        }

        private void RenderTaskCards()
        {
            flpTasks.SuspendLayout();
            flpTasks.Controls.Clear();

            string filter = txtSearch.Text.Trim().ToLowerInvariant();
            var filtered = _allTasks.Where(t =>
                string.IsNullOrEmpty(filter) ||
                (t.Key != null && t.Key.ToLowerInvariant().Contains(filter)) ||
                (t.Summary != null && t.Summary.ToLowerInvariant().Contains(filter)) ||
                (t.Status != null && t.Status.ToLowerInvariant().Contains(filter))
            ).ToList();

            int cardWidth = Math.Max(360, flpTasks.ClientSize.Width - 26);

            if (filtered.Count == 0)
            {
                var lblEmpty = new Label
                {
                    AutoSize = false,
                    Width = cardWidth,
                    Height = 80,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Font = new Font("Segoe UI", 9.5F),
                    Text = "Nenhuma atividade encontrada para o filtro selecionado."
                };
                flpTasks.Controls.Add(lblEmpty);
                flpTasks.ResumeLayout();
                return;
            }

            foreach (var task in filtered)
            {
                bool isRunning = _activeTask != null && _activeTask.Key == task.Key;
                var card = BuildTaskCard(task, isRunning, cardWidth);
                flpTasks.Controls.Add(card);
            }

            flpTasks.ResumeLayout();
        }

        private Panel BuildTaskCard(JiraTaskItem task, bool isRunning, int width)
        {
            Color bgCard = isRunning
                ? Color.FromArgb(22, 54, 47) // Destaque esmeralda escuro quando em contagem
                : Color.FromArgb(30, 38, 54);

            var card = new Panel
            {
                Width = width,
                Height = 88,
                BackColor = bgCard,
                Margin = new Padding(2, 4, 2, 4),
                Padding = new Padding(12, 8, 12, 8)
            };

            // Barra lateral colorida indicando estado
            var stripe = new Panel
            {
                Width = 4,
                Dock = DockStyle.Left,
                BackColor = isRunning
                    ? Color.FromArgb(16, 185, 129)
                    : Color.FromArgb(56, 189, 248)
            };

            // Chave da Tarefa + Projeto
            var lblKey = new Label
            {
                AutoSize = true,
                Location = new Point(14, 8),
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = isRunning ? Color.FromArgb(52, 211, 153) : Color.FromArgb(56, 189, 248),
                Text = $"{task.Key}"
            };

            // Badge de Status
            var lblStatus = new Label
            {
                AutoSize = true,
                Location = new Point(115, 10),
                Font = new Font("Segoe UI", 7.75F, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Text = $"• {task.Status} ({task.ProjectKey})"
            };

            // Resumo / Título da Tarefa
            var lblSummary = new Label
            {
                AutoEllipsis = true,
                Location = new Point(14, 30),
                Size = new Size(width - 135, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(241, 245, 249),
                Text = task.Summary
            };

            // Tempo total já registrado no Jira
            var lblTimeSpent = new Label
            {
                AutoSize = true,
                Location = new Point(14, 63),
                Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold),
                ForeColor = isRunning ? Color.FromArgb(110, 231, 183) : Color.FromArgb(148, 163, 184),
                Text = isRunning
                    ? $"⏱ No Jira: {task.FormattedTimeSpent} (Contando agora...)"
                    : $"⏱ Horas registradas no Jira: {task.FormattedTimeSpent}"
            };

            // Botão Iniciar / Parar
            var btnAction = new Button
            {
                Size = new Size(102, 36),
                Location = new Point(width - 114, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI Semibold", 8.75F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = isRunning
                    ? Color.FromArgb(220, 38, 38)
                    : Color.FromArgb(16, 185, 129),
                Text = isRunning ? "⏹ Parar" : "▶ Iniciar"
            };
            btnAction.FlatAppearance.BorderSize = 0;

            btnAction.Click += async (s, e) =>
            {
                if (isRunning)
                {
                    await StopAndSyncActiveTaskAsync("Parada manual pelo usuário");
                }
                else
                {
                    await StartOrSwitchTaskAsync(task);
                }
            };

            card.Controls.Add(btnAction);
            card.Controls.Add(lblKey);
            card.Controls.Add(lblStatus);
            card.Controls.Add(lblSummary);
            card.Controls.Add(lblTimeSpent);
            card.Controls.Add(stripe);

            return card;
        }

        /// <summary>
        /// Regra principal do projeto:
        /// - Ao escolher uma tarefa e clicar em Iniciar, a contagem de horas inicia.
        /// - Se alguém ligar para resolver algo e o usuário clicar em Iniciar em outra tarefa,
        ///   a contagem da tarefa anterior é interrompida e sincronizada no Jira automaticamente,
        ///   e inicia-se imediatamente uma nova contagem para a tarefa atual!
        /// </summary>
        private async Task StartOrSwitchTaskAsync(JiraTaskItem newTask)
        {
            JiraTaskItem previousTask = _activeTask;
            int previousElapsed = _elapsedSeconds;

            // 1. Inicia imediatamente o cronômetro para a nova tarefa selecionada
            _activeTask = newTask;
            _activeStartTimeUtc = DateTime.UtcNow;
            _elapsedSeconds = 0;
            tmrStopwatch.Start();

            UpdateActiveBannerUI();
            RenderTaskCards();

            // 2. Se havia uma tarefa anterior rodando (ex: interrupção por ligação),
            //    sincroniza o tempo da tarefa anterior em background no Jira!
            if (previousTask != null && previousTask.Key != newTask.Key)
            {
                SetStatus($"⚡ Interrompendo [{previousTask.Key}] ({JiraService.FormatDuration(previousElapsed)}) e sincronizando no Jira... Iniciando [{newTask.Key}]!", false);

                var syncResult = await _jiraService.LogWorkAsync(
                    _config,
                    previousTask.Key,
                    previousElapsed,
                    $"Troca automática de atividade para {newTask.Key} via Jira Focus Timer");

                if (syncResult.Success && !_config.UseSimulationMode)
                {
                    previousTask.TimeSpentSeconds += syncResult.SyncedSeconds;
                }

                RenderTaskCards();
                SetStatus($"⚡ {syncResult.Message} → Contando [{newTask.Key}]", !syncResult.Success);
            }
            else
            {
                SetStatus($"▶ Contagem iniciada para [{newTask.Key}] — {newTask.Summary}", false);
            }
        }

        private async void BtnStopActive_Click(object sender, EventArgs e)
        {
            await StopAndSyncActiveTaskAsync("Finalização pelo botão Parar");
        }

        private async Task StopAndSyncActiveTaskAsync(string reason)
        {
            if (_activeTask == null) return;

            tmrStopwatch.Stop();
            var stoppedTask = _activeTask;
            int totalElapsed = _elapsedSeconds;

            _activeTask = null;
            _elapsedSeconds = 0;

            UpdateActiveBannerUI();
            RenderTaskCards();

            SetStatus($"⏳ Sincronizando horas de [{stoppedTask.Key}] no Jira...", false);

            var syncResult = await _jiraService.LogWorkAsync(
                _config,
                stoppedTask.Key,
                totalElapsed,
                $"Apontamento via Jira Focus Timer ({reason})");

            if (syncResult.Success && !_config.UseSimulationMode)
            {
                stoppedTask.TimeSpentSeconds += syncResult.SyncedSeconds;
            }

            RenderTaskCards();
            SetStatus($"✓ {syncResult.Message}", !syncResult.Success);
        }

        private void TmrStopwatch_Tick(object sender, EventArgs e)
        {
            if (_activeTask == null)
            {
                tmrStopwatch.Stop();
                return;
            }

            _elapsedSeconds = (int)Math.Max(1, (DateTime.UtcNow - _activeStartTimeUtc).TotalSeconds);
            string clock = JiraService.FormatDigitalClock(_elapsedSeconds);
            lblTimerClock.Text = clock;

            string trayText = $"Jira Timer: {_activeTask.Key} ({clock})";
            notifyIcon.Text = trayText.Length > 63 ? trayText.Substring(0, 63) : trayText;
        }

        private void UpdateActiveBannerUI()
        {
            if (_activeTask != null)
            {
                pnlActiveTimer.BackColor = Color.FromArgb(20, 48, 42);
                lblActiveBadge.Text = "🟢 EM CONTAGEM ATIVA (CLIQUE EM OUTRA TAREFA PARA TROCAR E SINCRONIZAR)";
                lblActiveBadge.ForeColor = Color.FromArgb(52, 211, 153);

                lblActiveTaskKey.Text = $"{_activeTask.Key} ({_activeTask.ProjectKey})";
                lblActiveTaskKey.ForeColor = Color.White;

                lblActiveTaskTitle.Text = _activeTask.Summary;
                lblActiveTaskTitle.ForeColor = Color.FromArgb(203, 213, 225);

                lblTimerClock.Text = JiraService.FormatDigitalClock(_elapsedSeconds);
                lblTimerClock.ForeColor = Color.FromArgb(52, 211, 153);

                btnStopActive.Enabled = true;
                btnStopActive.BackColor = Color.FromArgb(220, 38, 38);
            }
            else
            {
                pnlActiveTimer.BackColor = Color.FromArgb(30, 38, 54);
                lblActiveBadge.Text = "⏸ NENHUMA TAREFA EM CONTAGEM";
                lblActiveBadge.ForeColor = Color.FromArgb(148, 163, 184);

                lblActiveTaskKey.Text = "Selecione uma tarefa abaixo";
                lblActiveTaskKey.ForeColor = Color.FromArgb(241, 245, 249);

                lblActiveTaskTitle.Text = "Se alguém ligar durante uma atividade, basta clicar em ▶ Iniciar na nova demanda para pausar e sincronizar a anterior.";
                lblActiveTaskTitle.ForeColor = Color.FromArgb(148, 163, 184);

                lblTimerClock.Text = "00:00:00";
                lblTimerClock.ForeColor = Color.FromArgb(100, 116, 139);

                btnStopActive.Enabled = false;
                btnStopActive.BackColor = Color.FromArgb(71, 85, 105);
                notifyIcon.Text = "Jira Focus Timer — Inativo";
            }
        }

        private void SetStatus(string message, bool isError)
        {
            lblSyncStatus.Text = message;
            lblSyncStatus.ForeColor = isError
                ? Color.FromArgb(248, 113, 113)
                : Color.FromArgb(52, 211, 153);
        }
    }
}
