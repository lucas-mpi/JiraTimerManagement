using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace JiraTimerManagement
{
    public partial class TelaPrincipal : Window
    {
        private readonly JiraService _jiraService = new JiraService();
        private JiraUserConfig _config;
        private List<JiraTaskItem> _allTasks = new List<JiraTaskItem>();

        private JiraTabMode _currentTab = JiraTabMode.MyTasks;
        private JiraTaskItem _activeTask = null;
        private DateTime _activeStartTimeUtc;
        private int _elapsedSeconds = 0;

        private bool _isMiniMode = false;
        private double _expandedHeight = 765;
        private bool _suppressProjectChange = false;
        private bool _isReady = false;

        private JiraTaskItem _manualModalTargetTask = null;
        private readonly DispatcherTimer _stopwatchTimer;
        private readonly DispatcherTimer _searchDebounceTimer;
        private readonly ObservableCollection<string> _syncLogs = new ObservableCollection<string>();
        private TextBlock _emptyStateTextBlock;

        public TelaPrincipal()
        {
            InitializeComponent();

            _syncLogs.Add("• Aguardando apontamentos (cronômetro ou manual)...");
            icSyncHistory.ItemsSource = _syncLogs;

            txtActiveTaskKey.MouseEnter += (s, e) =>
            {
                if (_activeTask != null)
                {
                    txtActiveTaskKey.TextDecorations = TextDecorations.Underline;
                }
            };
            txtActiveTaskKey.MouseLeave += (s, e) =>
            {
                txtActiveTaskKey.TextDecorations = null;
            };
            txtActiveTaskKey.MouseLeftButtonUp += (s, e) =>
            {
                if (_activeTask != null)
                {
                    OpenIssueInBrowser(_activeTask.Key);
                }
            };

            _stopwatchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _stopwatchTimer.Tick += StopwatchTimer_Tick;

            _searchDebounceTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(220)
            };
            _searchDebounceTimer.Tick += (s, e) =>
            {
                _searchDebounceTimer.Stop();
                ApplySearchFilter();
            };

            this.Loaded += TelaPrincipal_Loaded;
            this.Closing += TelaPrincipal_Closing;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                BtnMaximizeWindow_Click(sender, e);
                return;
            }
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void BtnMinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnMaximizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void BtnCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void TelaPrincipal_Loaded(object sender, RoutedEventArgs e)
        {
            _config = _jiraService.LoadConfig();

            inpUrl.Text = _config.BaseUrl;
            inpEmail.Text = _config.Email;
            inpToken.Password = _config.ApiToken;
            chkSimulation.IsChecked = _config.UseSimulationMode;

            UpdateCredentialInputsState();
            _isReady = true;

            await ConnectAndLoadAllAsync();
        }

        private async void TelaPrincipal_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_activeTask != null && _elapsedSeconds > 0)
            {
                var res = MessageBox.Show(
                    $"A atividade [{_activeTask.Key}] está com o cronômetro ativo ({JiraService.FormatDigitalClock(_elapsedSeconds)}).\n\nDeseja parar e sincronizar essas horas no Jira antes de fechar?",
                    "Sincronizar horas pendentes?",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (res == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (res == MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    await StopAndSyncActiveTaskAsync(inpActiveComment.Text.Trim());
                    this.Close();
                }
            }
        }

        private void ChkSimulation_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isReady) return;
            UpdateCredentialInputsState();
        }

        private void UpdateCredentialInputsState()
        {
            bool isReal = chkSimulation.IsChecked != true;
            inpUrl.IsEnabled = isReal;
            inpEmail.IsEnabled = isReal;
            inpToken.IsEnabled = isReal;
        }

        private void BtnTopMost_Click(object sender, RoutedEventArgs e)
        {
            this.Topmost = !this.Topmost;
            UpdateTopMostVisual();
            SetFooter(this.Topmost
                ? "📌 Janela fixada sempre no topo (ideal durante ligações e dailys)."
                : "Janela em modo normal.", false);
        }

        private void UpdateTopMostVisual()
        {
            runTopMostText.Text = this.Topmost ? "Fixado" : "Topo";
            btnTopMost.Style = (Style)FindResource(this.Topmost ? "PrimaryBtn" : "SecondaryBtn");
        }

        private void BtnMini_Click(object sender, RoutedEventArgs e)
        {
            _isMiniMode = !_isMiniMode;
            if (_isMiniMode)
            {
                _expandedHeight = this.Height;
                pnlAccount.Visibility = Visibility.Collapsed;
                pnlExpandableBody.Visibility = Visibility.Collapsed;
                pnlHistorySection.Visibility = Visibility.Collapsed;
                this.Height = 285;
                btnMini.Content = "🗖 Lista";
                this.Topmost = true;
                UpdateTopMostVisual();
            }
            else
            {
                pnlExpandableBody.Visibility = Visibility.Visible;
                pnlHistorySection.Visibility = Visibility.Visible;
                this.Height = Math.Max(680, _expandedHeight);
                btnMini.Content = "🗕 Mini";
            }
        }

        private void BtnConta_Click(object sender, RoutedEventArgs e)
        {
            if (_isMiniMode) BtnMini_Click(sender, e);
            pnlAccount.Visibility = pnlAccount.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private async void BtnSaveAccount_Click(object sender, RoutedEventArgs e)
        {
            _config.BaseUrl = inpUrl.Text.Trim();
            _config.Email = inpEmail.Text.Trim();
            _config.ApiToken = inpToken.Password.Trim();
            _config.UseSimulationMode = chkSimulation.IsChecked == true;

            _jiraService.SaveConfig(_config);
            pnlAccount.Visibility = Visibility.Collapsed;

            await ConnectAndLoadAllAsync();
        }

        private async void BtnTabMine_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTab == JiraTabMode.MyTasks) return;
            _currentTab = JiraTabMode.MyTasks;
            UpdateTabsVisual();
            await RefreshTasksAsync();
        }

        private async void BtnTabCeremonies_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTab == JiraTabMode.Ceremonies) return;
            _currentTab = JiraTabMode.Ceremonies;
            UpdateTabsVisual();
            await RefreshTasksAsync();
        }

        private async void BtnTabAllActive_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTab == JiraTabMode.AllActive) return;
            _currentTab = JiraTabMode.AllActive;
            UpdateTabsVisual();
            await RefreshTasksAsync();
        }

        private void UpdateTabsVisual()
        {
            btnTabMine.Style = (Style)FindResource(_currentTab == JiraTabMode.MyTasks ? "PrimaryBtn" : "SecondaryBtn");
            btnTabCeremonies.Style = (Style)FindResource(_currentTab == JiraTabMode.Ceremonies ? "PrimaryBtn" : "SecondaryBtn");
            btnTabAllActive.Style = (Style)FindResource(_currentTab == JiraTabMode.AllActive ? "PrimaryBtn" : "SecondaryBtn");
        }

        private async Task ConnectAndLoadAllAsync()
        {
            SetFooter("⏳ Autenticando conta e carregando projetos do Jira...", false);
            runUserDisplay.Text = "Autenticando...";

            var loginRes = await _jiraService.ValidateLoginAsync(_config);
            if (!loginRes.Success)
            {
                runUserDisplay.Text = "Não autenticado (clique em ⚙ Conta)";
                runUserDisplay.Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113));
                SetFooter(loginRes.ErrorMessage, true);
                pnlAccount.Visibility = Visibility.Visible;
                return;
            }

            string cleanHost = (_config.BaseUrl ?? "larcoop.atlassian.net")
                .Replace("https://", "")
                .Replace("http://", "")
                .TrimEnd('/');

            runUserDisplay.Text = _config.UseSimulationMode
                ? $"{_config.Email} ({cleanHost} • Protótipo)"
                : $"{_config.Email} ({cleanHost})";
            runUserDisplay.Foreground = new SolidColorBrush(Color.FromRgb(161, 161, 170));

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

        private async void CmbProjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isReady || _suppressProjectChange) return;

            if (cmbProjects.SelectedItem is JiraProjectItem proj)
            {
                _config.SelectedProjectKey = proj.Key;
                _jiraService.SaveConfig(_config);
                await RefreshTasksAsync();
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await RefreshTasksAsync();
        }

        private void InpSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isReady) return;
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        private async Task RefreshTasksAsync()
        {
            string projectKey = (cmbProjects.SelectedItem as JiraProjectItem)?.Key ?? "ALL";
            btnRefresh.IsEnabled = false;
            string tabName = _currentTab == JiraTabMode.MyTasks
                ? "Minhas Tarefas"
                : (_currentTab == JiraTabMode.Ceremonies ? "Cerimônias" : "Todos Ativos (Scrum & Sustentação)");
            SetFooter($"⏳ Buscando {tabName} ({(projectKey == "ALL" ? "Todos os projetos" : projectKey)})...", false);

            var (tasks, error) = await _jiraService.GetTasksForTabAsync(_config, projectKey, _currentTab);
            btnRefresh.IsEnabled = true;

            if (!string.IsNullOrEmpty(error))
            {
                SetFooter(error, true);
                return;
            }

            _allTasks = tasks;
            RenderTaskCards();
            SetFooter($"✓ {_allTasks.Count} card(s) em '{tabName}'. Use ▶ Iniciar ou + Manual / 💬.", false);
        }

        private void RenderTaskCards()
        {
            _searchDebounceTimer.Stop();
            spTasks.Children.Clear();

            _emptyStateTextBlock = new TextBlock
            {
                Text = "Nenhum card encontrado nesta aba/filtro.",
                Foreground = new SolidColorBrush(Color.FromRgb(161, 161, 170)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(12, 28, 12, 28),
                Visibility = Visibility.Collapsed
            };
            spTasks.Children.Add(_emptyStateTextBlock);

            foreach (var task in _allTasks)
            {
                bool isRunning = _activeTask != null && _activeTask.Key == task.Key;
                var cardElement = CreateCardElement(task, isRunning);
                cardElement.Tag = task;
                spTasks.Children.Add(cardElement);
            }

            ApplySearchFilter();
        }

        private void ApplySearchFilter()
        {
            string filter = (inpSearch.Text ?? "").Trim().ToLowerInvariant();
            int visibleCount = 0;

            foreach (UIElement child in spTasks.Children)
            {
                if (child is FrameworkElement fe && fe.Tag is JiraTaskItem t)
                {
                    bool matches = string.IsNullOrEmpty(filter) ||
                        (t.Key != null && t.Key.ToLowerInvariant().Contains(filter)) ||
                        (t.Summary != null && t.Summary.ToLowerInvariant().Contains(filter)) ||
                        (t.Status != null && t.Status.ToLowerInvariant().Contains(filter)) ||
                        (t.Assignee != null && t.Assignee.ToLowerInvariant().Contains(filter));

                    fe.Visibility = matches ? Visibility.Visible : Visibility.Collapsed;
                    if (matches) visibleCount++;
                }
            }

            if (_emptyStateTextBlock != null)
            {
                _emptyStateTextBlock.Visibility = visibleCount == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private FrameworkElement CreateCardElement(JiraTaskItem task, bool isRunning)
        {
            var cardBorder = new Border
            {
                Background = new SolidColorBrush(isRunning ? Color.FromRgb(24, 30, 38) : Color.FromRgb(24, 24, 28)),
                BorderBrush = new SolidColorBrush(isRunning ? Color.FromRgb(2, 132, 199) : Color.FromRgb(46, 46, 52)),
                BorderThickness = new Thickness(isRunning ? 1.5 : 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Coluna Esquerda (Informações do Card)
            var leftStack = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };

            // Linha 1: Código (Hyperlink para abrir no navegador) + Status
            var headerWrap = new StackPanel { Orientation = Orientation.Horizontal };

            var keyText = new TextBlock
            {
                Text = task.Key,
                FontSize = 12.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(14, 165, 233)),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 6, 0),
                ToolTip = $"Abrir {task.Key} no navegador"
            };
            keyText.MouseEnter += (s, e) =>
            {
                keyText.TextDecorations = TextDecorations.Underline;
                keyText.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248));
            };
            keyText.MouseLeave += (s, e) =>
            {
                keyText.TextDecorations = null;
                keyText.Foreground = new SolidColorBrush(Color.FromRgb(14, 165, 233));
            };
            keyText.MouseLeftButtonUp += (s, e) => OpenIssueInBrowser(task.Key);

            var statusText = new TextBlock
            {
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(161, 161, 170)),
                VerticalAlignment = VerticalAlignment.Center
            };
            if (task.IsCeremonyOrShared)
            {
                statusText.Inlines.Add(new Run("• "));
                statusText.Inlines.Add(new Run("🤝 ") { Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36)) });
                statusText.Inlines.Add(new Run($"Cerimônia/Time ({task.ProjectKey})"));
            }
            else
            {
                statusText.Text = $"• {task.Status} ({task.ProjectKey})";
            }

            headerWrap.Children.Add(keyText);
            headerWrap.Children.Add(statusText);

            // Linha 2: Resumo / Título
            var summaryText = new TextBlock
            {
                Text = task.Summary,
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromRgb(244, 244, 245)),
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 36,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 4, 0, 6)
            };

            // Linha 3: Tempo gasto no Jira + Último comentário
            var timeLine = new TextBlock
            {
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(161, 161, 170)),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            timeLine.Inlines.Add(new Run("⏱ No Jira: "));
            timeLine.Inlines.Add(new Run(task.FormattedTimeSpent)
            {
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(244, 244, 245))
            });

            if (isRunning)
            {
                timeLine.Inlines.Add(new Run(" (Contando...)")
                {
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(14, 165, 233))
                });
            }
            else if (!string.IsNullOrWhiteSpace(task.LastComment))
            {
                timeLine.Inlines.Add(new Run(" • 💬 ") { Foreground = new SolidColorBrush(Color.FromRgb(244, 244, 245)) });
                timeLine.Inlines.Add(new Run($"\"{task.LastComment}\""));
            }

            leftStack.Children.Add(headerWrap);
            leftStack.Children.Add(summaryText);
            leftStack.Children.Add(timeLine);

            // Coluna Direita (Botões Iniciar/Parar e + Manual / 💬)
            var rightStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Width = 104 };

            var btnAction = new Button
            {
                Content = isRunning ? "■ Parar" : "▶ Iniciar",
                Style = (Style)FindResource(isRunning ? "DangerBtn" : "PrimaryBtn"),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 0, 6)
            };
            btnAction.Click += async (s, e) =>
            {
                if (isRunning)
                {
                    await StopAndSyncActiveTaskAsync(inpActiveComment.Text.Trim());
                }
                else
                {
                    await StartOrSwitchTaskAsync(task);
                }
            };

            var btnManual = new Button
            {
                Content = "+ Manual / 💬",
                Style = (Style)FindResource("SecondaryBtn"),
                FontSize = 11,
                Padding = new Thickness(8, 5, 8, 5)
            };
            btnManual.Click += (s, e) => OpenManualModal(task);

            rightStack.Children.Add(btnAction);
            rightStack.Children.Add(btnManual);

            Grid.SetColumn(leftStack, 0);
            Grid.SetColumn(rightStack, 1);
            grid.Children.Add(leftStack);
            grid.Children.Add(rightStack);

            cardBorder.Child = grid;
            return cardBorder;
        }

        private void OpenIssueInBrowser(string issueKey)
        {
            if (string.IsNullOrWhiteSpace(issueKey)) return;
            try
            {
                string baseUrl = (_config?.BaseUrl ?? "https://larcoop.atlassian.net").TrimEnd('/');
                string url = $"{baseUrl}/browse/{Uri.EscapeDataString(issueKey.Trim())}";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                SetFooter($"Falha ao abrir navegador: {ex.Message}", true);
            }
        }

        // --- Modal Lançamento Manual ---
        private void OpenManualModal(JiraTaskItem task)
        {
            _manualModalTargetTask = task;
            txtModalTitle.Text = $"Lançar Horas Manuais — {task.Key}";
            txtModalSummary.Text = task.Summary;
            inpManualDuration.Text = "15m";
            inpManualComment.Text = task.IsCeremonyOrShared
                ? "Participação na cerimônia / alinhamento técnico da equipe"
                : "";
            modalManualOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCloseManualModal_Click(object sender, RoutedEventArgs e)
        {
            modalManualOverlay.Visibility = Visibility.Collapsed;
            _manualModalTargetTask = null;
        }

        private void BtnPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string preset)
            {
                inpManualDuration.Text = preset;
            }
        }

        private async void BtnConfirmManualModal_Click(object sender, RoutedEventArgs e)
        {
            if (_manualModalTargetTask == null) return;

            int seconds = JiraService.ParseDurationToSeconds(inpManualDuration.Text);
            if (seconds <= 0)
            {
                MessageBox.Show("Informe um tempo válido (ex: 15m, 30m, 1h 30m ou 45).", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var target = _manualModalTargetTask;
            string comment = inpManualComment.Text.Trim();
            modalManualOverlay.Visibility = Visibility.Collapsed;
            _manualModalTargetTask = null;

            SetFooter($"⏳ Lançando +{JiraService.FormatDuration(seconds)} manualmente em [{target.Key}]...", false);

            var syncResult = await _jiraService.LogWorkAsync(_config, target.Key, seconds, comment);
            if (syncResult.Success && !_config.UseSimulationMode)
            {
                target.TimeSpentSeconds += syncResult.SyncedSeconds;
                if (!string.IsNullOrWhiteSpace(comment))
                    target.LastComment = comment;
            }

            AppendSyncLog(target.Key, seconds, syncResult.SyncedSeconds, syncResult.Comment, true);
            RenderTaskCards();
            SetFooter($"✓ {syncResult.Message}", !syncResult.Success);
        }

        // --- Fluxo do Cronômetro Ativo e Troca com 1 Clique ---
        private async Task StartOrSwitchTaskAsync(JiraTaskItem newTask)
        {
            JiraTaskItem previousTask = _activeTask;
            int previousElapsed = _elapsedSeconds;
            string previousComment = inpActiveComment.Text.Trim();

            _activeTask = newTask;
            _activeStartTimeUtc = DateTime.UtcNow;
            _elapsedSeconds = 0;
            inpActiveComment.Text = "";
            _stopwatchTimer.Start();

            UpdateActiveBannerUI();
            RenderTaskCards();

            if (previousTask != null && previousTask.Key != newTask.Key)
            {
                string autoNote = string.IsNullOrWhiteSpace(previousComment)
                    ? $"Troca automática para {newTask.Key}"
                    : previousComment;

                SetFooter($"⚡ Pausando [{previousTask.Key}] ({JiraService.FormatDuration(previousElapsed)}) e sincronizando no Jira... Iniciando [{newTask.Key}]!", false);

                var syncResult = await _jiraService.LogWorkAsync(_config, previousTask.Key, previousElapsed, autoNote);
                if (syncResult.Success && !_config.UseSimulationMode)
                {
                    previousTask.TimeSpentSeconds += syncResult.SyncedSeconds;
                    if (!string.IsNullOrWhiteSpace(previousComment))
                        previousTask.LastComment = previousComment;
                }

                AppendSyncLog(previousTask.Key, previousElapsed, syncResult.SyncedSeconds, syncResult.Comment, false);
                RenderTaskCards();
                SetFooter($"⚡ {syncResult.Message} → Contando [{newTask.Key}]", !syncResult.Success);
            }
            else
            {
                SetFooter($"▶ Contagem iniciada em [{newTask.Key}].", false);
            }
        }

        private async void BtnStopMain_Click(object sender, RoutedEventArgs e)
        {
            await StopAndSyncActiveTaskAsync(inpActiveComment.Text.Trim());
        }

        private async Task StopAndSyncActiveTaskAsync(string userComment)
        {
            if (_activeTask == null) return;

            _stopwatchTimer.Stop();
            var stoppedTask = _activeTask;
            int totalElapsed = _elapsedSeconds;

            _activeTask = null;
            _elapsedSeconds = 0;
            inpActiveComment.Text = "";

            UpdateActiveBannerUI();
            RenderTaskCards();

            SetFooter($"⏳ Sincronizando horas de [{stoppedTask.Key}] no Jira...", false);

            var syncResult = await _jiraService.LogWorkAsync(_config, stoppedTask.Key, totalElapsed, userComment);
            if (syncResult.Success && !_config.UseSimulationMode)
            {
                stoppedTask.TimeSpentSeconds += syncResult.SyncedSeconds;
                if (!string.IsNullOrWhiteSpace(userComment))
                    stoppedTask.LastComment = userComment;
            }

            AppendSyncLog(stoppedTask.Key, totalElapsed, syncResult.SyncedSeconds, syncResult.Comment, false);
            RenderTaskCards();
            SetFooter($"✓ {syncResult.Message}", !syncResult.Success);
        }

        private void AppendSyncLog(string issueKey, int rawSeconds, int syncedSeconds, string comment, bool isManual)
        {
            if (_syncLogs.Count == 1 && _syncLogs[0].StartsWith("•"))
            {
                _syncLogs.Clear();
            }

            string timeStr = DateTime.Now.ToString("HH:mm:ss");
            string modeTag = isManual
                ? "Manual"
                : (rawSeconds < 60 ? $"Arred. de {rawSeconds}s" : "Cronômetro");

            _syncLogs.Insert(0, $"✓ {timeStr} — [{issueKey}] +{JiraService.FormatDuration(syncedSeconds)} ({modeTag}) • 💬 \"{comment}\"");
        }

        private void StopwatchTimer_Tick(object sender, EventArgs e)
        {
            if (_activeTask == null)
            {
                _stopwatchTimer.Stop();
                return;
            }

            _elapsedSeconds = (int)Math.Max(1, (DateTime.UtcNow - _activeStartTimeUtc).TotalSeconds);
            txtDigitalClock.Text = JiraService.FormatDigitalClock(_elapsedSeconds);
        }

        private void UpdateActiveBannerUI()
        {
            if (_activeTask != null)
            {
                bdrActiveBanner.Background = new SolidColorBrush(Color.FromRgb(20, 30, 38));
                txtActiveStatusBadge.Text = "🟢 EM CONTAGEM ATIVA (CLIQUE EM OUTRA TAREFA SE ALGUÉM LIGAR)";
                txtActiveStatusBadge.Foreground = new SolidColorBrush(Color.FromRgb(14, 165, 233));

                txtActiveTaskKey.Text = $"{_activeTask.Key} ({_activeTask.ProjectKey})";
                txtActiveTaskKey.Foreground = new SolidColorBrush(Color.FromRgb(14, 165, 233));
                txtActiveTaskKey.Cursor = Cursors.Hand;
                txtActiveTaskKey.ToolTip = $"Abrir {_activeTask.Key} no navegador";
                txtActiveTaskSummary.Text = _activeTask.Summary;

                txtDigitalClock.Text = JiraService.FormatDigitalClock(_elapsedSeconds);
                txtDigitalClock.Foreground = new SolidColorBrush(Color.FromRgb(14, 165, 233));

                btnStopMain.IsEnabled = true;
                btnStopMain.Style = (Style)FindResource("DangerBtn");
            }
            else
            {
                bdrActiveBanner.Background = new SolidColorBrush(Color.FromRgb(24, 24, 28));
                txtActiveStatusBadge.Text = "⏸ NENHUMA TAREFA EM CONTAGEM";
                txtActiveStatusBadge.Foreground = new SolidColorBrush(Color.FromRgb(161, 161, 170));

                txtActiveTaskKey.Text = "Selecione uma tarefa ou cerimônia abaixo";
                txtActiveTaskKey.Foreground = new SolidColorBrush(Color.FromRgb(244, 244, 245));
                txtActiveTaskKey.Cursor = Cursors.Arrow;
                txtActiveTaskKey.ToolTip = null;
                txtActiveTaskSummary.Text = "Ao clicar em ▶ Iniciar em outra atividade, a anterior é pausada e sincronizada com o comentário.";

                txtDigitalClock.Text = "00:00:00";
                txtDigitalClock.Foreground = new SolidColorBrush(Color.FromRgb(161, 161, 170));

                btnStopMain.IsEnabled = false;
                btnStopMain.Style = (Style)FindResource("SecondaryBtn");
            }
        }

        private void SetFooter(string message, bool isError)
        {
            txtFooterStatus.Text = message;
            txtFooterStatus.Foreground = new SolidColorBrush(
                isError ? Color.FromRgb(248, 113, 113) : Color.FromRgb(228, 228, 231));
        }
    }
}
