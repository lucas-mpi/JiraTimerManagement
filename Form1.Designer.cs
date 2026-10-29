using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace JiraTimerManagement
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        // Controles principais de topo
        private Panel pnlHeader;
        private Label lblAppTitle;
        private Label lblUserStatus;
        private Button btnTopMost;
        private Button btnToggleMini;
        private Button btnToggleConfig;

        // Painel retrátil de Login / Configuração Jira
        private Panel pnlConfig;
        private Label lblConfigTitle;
        private CheckBox chkSimulationMode;
        private Label lblUrl;
        private Panel pnlUrlWrap;
        private TextBox txtBaseUrl;
        private Label lblEmail;
        private Panel pnlEmailWrap;
        private TextBox txtEmail;
        private Label lblToken;
        private Panel pnlTokenWrap;
        private TextBox txtApiToken;
        private Button btnSaveConnect;

        // Painel do Cronômetro Ativo + Comentário Rápido
        private Panel pnlActiveTimer;
        private Label lblActiveBadge;
        private Label lblActiveTaskKey;
        private Label lblActiveTaskTitle;
        private Label lblTimerClock;
        private Button btnStopActive;
        private Label lblCommentIcon;
        private Panel pnlActiveCommentWrap;
        private TextBox txtActiveComment;

        // Painel de Abas (Minhas Tarefas vs Cerimônias/Fixadas) + Filtros
        private Panel pnlFilters;
        private Button btnTabMyTasks;
        private Button btnTabCeremonies;
        private Button btnPinNewCard;
        private Label lblProjectFilter;
        private ComboBox cmbProjects;
        private Panel pnlSearchWrap;
        private TextBox txtSearch;
        private Button btnRefresh;

        // Lista de Cards de Tarefas
        private FlowLayoutPanel flpTasks;

        // Painel de Histórico de Apontamentos Sincronizados (igual ao protótipo)
        private Panel pnlHistory;
        private Label lblHistoryHeader;
        private Label lblHistoryRule;
        private ListBox lstSyncHistory;

        // Barra de Status Inferior
        private Panel pnlFooter;
        private Label lblSyncStatus;

        // Timer de contagem (1 segundo) e Ícone de Bandeja
        private Timer tmrStopwatch;
        private NotifyIcon notifyIcon;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.pnlHeader = new Panel();
            this.lblAppTitle = new Label();
            this.lblUserStatus = new Label();
            this.btnTopMost = new Button();
            this.btnToggleMini = new Button();
            this.btnToggleConfig = new Button();

            this.pnlConfig = new Panel();
            this.lblConfigTitle = new Label();
            this.chkSimulationMode = new CheckBox();
            this.lblUrl = new Label();
            this.pnlUrlWrap = new Panel();
            this.txtBaseUrl = new TextBox();
            this.lblEmail = new Label();
            this.pnlEmailWrap = new Panel();
            this.txtEmail = new TextBox();
            this.lblToken = new Label();
            this.pnlTokenWrap = new Panel();
            this.txtApiToken = new TextBox();
            this.btnSaveConnect = new Button();

            this.pnlActiveTimer = new Panel();
            this.lblActiveBadge = new Label();
            this.lblActiveTaskKey = new Label();
            this.lblActiveTaskTitle = new Label();
            this.lblTimerClock = new Label();
            this.btnStopActive = new Button();
            this.lblCommentIcon = new Label();
            this.pnlActiveCommentWrap = new Panel();
            this.txtActiveComment = new TextBox();

            this.pnlFilters = new Panel();
            this.btnTabMyTasks = new Button();
            this.btnTabCeremonies = new Button();
            this.btnPinNewCard = new Button();
            this.lblProjectFilter = new Label();
            this.cmbProjects = new ComboBox();
            this.pnlSearchWrap = new Panel();
            this.txtSearch = new TextBox();
            this.btnRefresh = new Button();

            this.flpTasks = new FlowLayoutPanel();

            this.pnlHistory = new Panel();
            this.lblHistoryHeader = new Label();
            this.lblHistoryRule = new Label();
            this.lstSyncHistory = new ListBox();

            this.pnlFooter = new Panel();
            this.lblSyncStatus = new Label();

            this.tmrStopwatch = new Timer(this.components);
            this.notifyIcon = new NotifyIcon(this.components);

            this.SuspendLayout();

            // Paleta idêntica ao protótipo aprovado
            Color bgMain = Color.FromArgb(15, 18, 25);       // Fundo principal super escuro
            Color bgHeader = Color.FromArgb(20, 24, 34);     // Barra superior / rodapé
            Color bgBanner = Color.FromArgb(22, 27, 38);     // Painel do cronômetro ativo
            Color bgCard = Color.FromArgb(26, 32, 44);       // Fundo de cards e botões secundários
            Color bgInput = Color.FromArgb(17, 21, 30);      // Caixas de texto
            Color borderSlate = Color.FromArgb(45, 55, 72);  // Bordas sutis
            Color fgPrimary = Color.FromArgb(241, 245, 249); // Texto claro
            Color fgMuted = Color.FromArgb(148, 163, 184);   // Texto secundário
            Color primaryBlue = Color.FromArgb(14, 165, 233);// Azul destaque

            // 
            // Form1
            // 
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.BackColor = bgMain;
            this.ClientSize = new Size(470, 760);
            this.MinimumSize = new Size(450, 250);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.ForeColor = fgPrimary;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Jira Focus Timer — Tarefas & Cerimônias";
            this.DoubleBuffered = true;

            // 
            // 1. pnlHeader (Topo)
            // 
            this.pnlHeader.BackColor = bgHeader;
            this.pnlHeader.Dock = DockStyle.Top;
            this.pnlHeader.Size = new Size(470, 56);
            this.pnlHeader.Paint += (s, e) => DrawBottomBorder(e.Graphics, this.pnlHeader, borderSlate);

            this.lblAppTitle.AutoSize = true;
            this.lblAppTitle.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            this.lblAppTitle.ForeColor = primaryBlue;
            this.lblAppTitle.Location = new Point(14, 8);
            this.lblAppTitle.Text = "⏱ Jira Focus Timer";

            this.lblUserStatus.AutoSize = true;
            this.lblUserStatus.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular);
            this.lblUserStatus.ForeColor = fgMuted;
            this.lblUserStatus.Location = new Point(14, 30);
            this.lblUserStatus.Text = "Conectando...";

            StyleSecondaryButton(this.btnTopMost, "📌 Topo", new Size(68, 28), bgCard, borderSlate, fgPrimary);
            this.btnTopMost.Click += this.BtnTopMost_Click;

            StyleSecondaryButton(this.btnToggleMini, "🗕 Mini", new Size(64, 28), bgCard, borderSlate, fgPrimary);
            this.btnToggleMini.Click += this.BtnToggleMini_Click;

            StyleSecondaryButton(this.btnToggleConfig, "⚙ Conta", new Size(68, 28), bgCard, borderSlate, fgPrimary);
            this.btnToggleConfig.Click += this.BtnToggleConfig_Click;

            this.pnlHeader.Controls.Add(this.lblAppTitle);
            this.pnlHeader.Controls.Add(this.lblUserStatus);
            this.pnlHeader.Controls.Add(this.btnTopMost);
            this.pnlHeader.Controls.Add(this.btnToggleMini);
            this.pnlHeader.Controls.Add(this.btnToggleConfig);

            // 
            // 2. pnlConfig (Painel Retrátil de Conta Jira)
            // 
            this.pnlConfig.BackColor = bgBanner;
            this.pnlConfig.Dock = DockStyle.Top;
            this.pnlConfig.Size = new Size(470, 190);
            this.pnlConfig.Visible = false;
            this.pnlConfig.Paint += (s, e) => DrawBottomBorder(e.Graphics, this.pnlConfig, borderSlate);

            this.lblConfigTitle.AutoSize = true;
            this.lblConfigTitle.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            this.lblConfigTitle.ForeColor = fgPrimary;
            this.lblConfigTitle.Location = new Point(14, 10);
            this.lblConfigTitle.Text = "Autenticação Jira Cloud (REST API v3)";

            this.chkSimulationMode.AutoSize = true;
            this.chkSimulationMode.Font = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold);
            this.chkSimulationMode.ForeColor = Color.FromArgb(250, 204, 21);
            this.chkSimulationMode.Location = new Point(14, 32);
            this.chkSimulationMode.Text = "Usar Modo Protótipo (Dados Simulados para Teste Rápido)";
            this.chkSimulationMode.CheckedChanged += this.ChkSimulationMode_CheckedChanged;

            this.lblUrl.AutoSize = true;
            this.lblUrl.ForeColor = fgMuted;
            this.lblUrl.Location = new Point(14, 62);
            this.lblUrl.Text = "URL Jira:";
            CreateFlatTextBox(this.pnlUrlWrap, this.txtBaseUrl, new Point(92, 58), new Size(364, 26), bgInput, borderSlate, fgPrimary);

            this.lblEmail.AutoSize = true;
            this.lblEmail.ForeColor = fgMuted;
            this.lblEmail.Location = new Point(14, 94);
            this.lblEmail.Text = "E-mail:";
            CreateFlatTextBox(this.pnlEmailWrap, this.txtEmail, new Point(92, 90), new Size(364, 26), bgInput, borderSlate, fgPrimary);

            this.lblToken.AutoSize = true;
            this.lblToken.ForeColor = fgMuted;
            this.lblToken.Location = new Point(14, 126);
            this.lblToken.Text = "API Token:";
            CreateFlatTextBox(this.pnlTokenWrap, this.txtApiToken, new Point(92, 122), new Size(364, 26), bgInput, borderSlate, fgPrimary);
            this.txtApiToken.PasswordChar = '•';

            this.btnSaveConnect.BackColor = Color.FromArgb(2, 132, 199);
            this.btnSaveConnect.FlatStyle = FlatStyle.Flat;
            this.btnSaveConnect.FlatAppearance.BorderSize = 0;
            this.btnSaveConnect.Font = new Font("Segoe UI Semibold", 8.75F, FontStyle.Bold);
            this.btnSaveConnect.ForeColor = Color.White;
            this.btnSaveConnect.Location = new Point(92, 154);
            this.btnSaveConnect.Size = new Size(364, 28);
            this.btnSaveConnect.Text = "Salvar Credenciais e Carregar Projetos";
            this.btnSaveConnect.Cursor = Cursors.Hand;
            this.btnSaveConnect.Click += this.BtnSaveConnect_Click;

            this.pnlConfig.Controls.Add(this.lblConfigTitle);
            this.pnlConfig.Controls.Add(this.chkSimulationMode);
            this.pnlConfig.Controls.Add(this.lblUrl);
            this.pnlConfig.Controls.Add(this.pnlUrlWrap);
            this.pnlConfig.Controls.Add(this.lblEmail);
            this.pnlConfig.Controls.Add(this.pnlEmailWrap);
            this.pnlConfig.Controls.Add(this.lblToken);
            this.pnlConfig.Controls.Add(this.pnlTokenWrap);
            this.pnlConfig.Controls.Add(this.btnSaveConnect);

            // 
            // 3. pnlActiveTimer (Painel do Cronômetro Ativo + Comentário Rápido)
            // 
            this.pnlActiveTimer.BackColor = bgBanner;
            this.pnlActiveTimer.Dock = DockStyle.Top;
            this.pnlActiveTimer.Size = new Size(470, 128);
            this.pnlActiveTimer.Paint += (s, e) => DrawBottomBorder(e.Graphics, this.pnlActiveTimer, borderSlate);

            this.lblActiveBadge.AutoSize = true;
            this.lblActiveBadge.Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold);
            this.lblActiveBadge.ForeColor = fgMuted;
            this.lblActiveBadge.Location = new Point(14, 12);
            this.lblActiveBadge.Text = "II  NENHUMA TAREFA EM CONTAGEM";

            this.lblTimerClock.Font = new Font("Consolas", 16.5F, FontStyle.Bold);
            this.lblTimerClock.ForeColor = fgMuted;
            this.lblTimerClock.Size = new Size(125, 28);
            this.lblTimerClock.TextAlign = ContentAlignment.MiddleRight;
            this.lblTimerClock.Text = "00:00:00";

            this.lblActiveTaskKey.AutoEllipsis = true;
            this.lblActiveTaskKey.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblActiveTaskKey.ForeColor = fgPrimary;
            this.lblActiveTaskKey.Location = new Point(14, 38);
            this.lblActiveTaskKey.Size = new Size(300, 20);
            this.lblActiveTaskKey.Text = "Selecione uma tarefa ou cerimônia abaixo";

            this.lblActiveTaskTitle.AutoEllipsis = true;
            this.lblActiveTaskTitle.Font = new Font("Segoe UI", 8.25F);
            this.lblActiveTaskTitle.ForeColor = fgMuted;
            this.lblActiveTaskTitle.Location = new Point(14, 59);
            this.lblActiveTaskTitle.Size = new Size(300, 20);
            this.lblActiveTaskTitle.Text = "Ao clicar em ▶ Iniciar em outra atividade, a anterior é sincronizada.";

            StyleSecondaryButton(this.btnStopActive, "■ Parar & Sincronizar", new Size(136, 32), bgCard, borderSlate, fgMuted);
            this.btnStopActive.Enabled = false;
            this.btnStopActive.Click += this.BtnStopActive_Click;

            this.lblCommentIcon.AutoSize = true;
            this.lblCommentIcon.Font = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold);
            this.lblCommentIcon.ForeColor = fgMuted;
            this.lblCommentIcon.Location = new Point(14, 95);
            this.lblCommentIcon.Text = "Comentário:";

            CreateFlatTextBox(this.pnlActiveCommentWrap, this.txtActiveComment, new Point(92, 90), new Size(364, 26), bgInput, borderSlate, fgPrimary);

            this.pnlActiveTimer.Controls.Add(this.lblActiveBadge);
            this.pnlActiveTimer.Controls.Add(this.lblTimerClock);
            this.pnlActiveTimer.Controls.Add(this.lblActiveTaskKey);
            this.pnlActiveTimer.Controls.Add(this.lblActiveTaskTitle);
            this.pnlActiveTimer.Controls.Add(this.btnStopActive);
            this.pnlActiveTimer.Controls.Add(this.lblCommentIcon);
            this.pnlActiveTimer.Controls.Add(this.pnlActiveCommentWrap);

            // 
            // 4. pnlFilters (Abas + Seletor de Projeto + Busca)
            // 
            this.pnlFilters.BackColor = bgMain;
            this.pnlFilters.Dock = DockStyle.Top;
            this.pnlFilters.Size = new Size(470, 114);
            this.pnlFilters.Paint += (s, e) => DrawBottomBorder(e.Graphics, this.pnlFilters, borderSlate);

            this.btnTabMyTasks.BackColor = Color.FromArgb(2, 132, 199);
            this.btnTabMyTasks.FlatStyle = FlatStyle.Flat;
            this.btnTabMyTasks.FlatAppearance.BorderSize = 0;
            this.btnTabMyTasks.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            this.btnTabMyTasks.ForeColor = Color.White;
            this.btnTabMyTasks.Location = new Point(14, 10);
            this.btnTabMyTasks.Size = new Size(155, 32);
            this.btnTabMyTasks.Text = "Minhas Tarefas";
            this.btnTabMyTasks.Cursor = Cursors.Hand;
            this.btnTabMyTasks.Click += this.BtnTabMyTasks_Click;

            this.btnTabCeremonies.BackColor = bgCard;
            this.btnTabCeremonies.FlatStyle = FlatStyle.Flat;
            this.btnTabCeremonies.FlatAppearance.BorderColor = borderSlate;
            this.btnTabCeremonies.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            this.btnTabCeremonies.ForeColor = fgMuted;
            this.btnTabCeremonies.Location = new Point(175, 10);
            this.btnTabCeremonies.Size = new Size(180, 32);
            this.btnTabCeremonies.Text = "★ Cerimônias / Fixadas";
            this.btnTabCeremonies.Cursor = Cursors.Hand;
            this.btnTabCeremonies.Click += this.BtnTabCeremonies_Click;

            StyleSecondaryButton(this.btnPinNewCard, "+ Fixar Card", new Size(95, 32), bgCard, borderSlate, fgPrimary);
            this.btnPinNewCard.Click += this.BtnPinNewCard_Click;

            this.lblProjectFilter.AutoSize = true;
            this.lblProjectFilter.Font = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold);
            this.lblProjectFilter.ForeColor = fgMuted;
            this.lblProjectFilter.Location = new Point(14, 52);
            this.lblProjectFilter.Text = "Projeto / Time:";

            this.cmbProjects.BackColor = bgInput;
            this.cmbProjects.ForeColor = fgPrimary;
            this.cmbProjects.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbProjects.FlatStyle = FlatStyle.Flat;
            this.cmbProjects.Font = new Font("Segoe UI", 8.75F);
            this.cmbProjects.Location = new Point(102, 48);
            this.cmbProjects.Size = new Size(354, 24);
            this.cmbProjects.SelectedIndexChanged += this.CmbProjects_SelectedIndexChanged;

            CreateFlatTextBox(this.pnlSearchWrap, this.txtSearch, new Point(14, 80), new Size(350, 26), bgInput, borderSlate, fgPrimary);
            this.txtSearch.TextChanged += this.TxtSearch_TextChanged;

            StyleSecondaryButton(this.btnRefresh, "↻ Atualizar", new Size(86, 26), bgCard, borderSlate, fgPrimary);
            this.btnRefresh.Click += this.BtnRefresh_Click;

            this.pnlFilters.Controls.Add(this.btnTabMyTasks);
            this.pnlFilters.Controls.Add(this.btnTabCeremonies);
            this.pnlFilters.Controls.Add(this.btnPinNewCard);
            this.pnlFilters.Controls.Add(this.lblProjectFilter);
            this.pnlFilters.Controls.Add(this.cmbProjects);
            this.pnlFilters.Controls.Add(this.pnlSearchWrap);
            this.pnlFilters.Controls.Add(this.btnRefresh);

            // 
            // 5. pnlFooter (Barra de Status Inferior)
            // 
            this.pnlFooter.BackColor = bgHeader;
            this.pnlFooter.Dock = DockStyle.Bottom;
            this.pnlFooter.Size = new Size(470, 32);
            this.pnlFooter.Padding = new Padding(12, 4, 12, 4);

            this.lblSyncStatus.AutoEllipsis = true;
            this.lblSyncStatus.Dock = DockStyle.Fill;
            this.lblSyncStatus.Font = new Font("Segoe UI", 8.25F);
            this.lblSyncStatus.ForeColor = Color.FromArgb(52, 211, 153);
            this.lblSyncStatus.TextAlign = ContentAlignment.MiddleLeft;
            this.lblSyncStatus.Text = "✓ Pronto. Use ▶ Iniciar para contar ou + Manual para lançar horas.";

            this.pnlFooter.Controls.Add(this.lblSyncStatus);

            // 
            // 6. pnlHistory (Últimos Apontamentos Sincronizados — igual ao protótipo!)
            // 
            this.pnlHistory.BackColor = bgBanner;
            this.pnlHistory.Dock = DockStyle.Bottom;
            this.pnlHistory.Size = new Size(470, 92);
            this.pnlHistory.Paint += (s, e) =>
            {
                using (var pen = new Pen(borderSlate, 1))
                {
                    e.Graphics.DrawLine(pen, 0, 0, this.pnlHistory.Width, 0);
                    e.Graphics.DrawLine(pen, 0, this.pnlHistory.Height - 1, this.pnlHistory.Width, this.pnlHistory.Height - 1);
                }
            };

            this.lblHistoryHeader.AutoSize = true;
            this.lblHistoryHeader.Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold);
            this.lblHistoryHeader.ForeColor = fgMuted;
            this.lblHistoryHeader.Location = new Point(14, 7);
            this.lblHistoryHeader.Text = "Últimos Apontamentos Sincronizados (Worklog + Comentário)";

            this.lblHistoryRule.AutoSize = true;
            this.lblHistoryRule.Font = new Font("Segoe UI", 7.75F);
            this.lblHistoryRule.ForeColor = fgMuted;
            this.lblHistoryRule.Text = "Mínimo Jira: 1m (60s)";

            this.lstSyncHistory.BackColor = bgBanner;
            this.lstSyncHistory.BorderStyle = BorderStyle.None;
            this.lstSyncHistory.ForeColor = fgPrimary;
            this.lstSyncHistory.Font = new Font("Segoe UI", 8.25F);
            this.lstSyncHistory.Location = new Point(14, 28);
            this.lstSyncHistory.Size = new Size(442, 56);
            this.lstSyncHistory.Items.Add("• Aguardando apontamentos (cronômetro ou manual)...");

            this.pnlHistory.Controls.Add(this.lblHistoryHeader);
            this.pnlHistory.Controls.Add(this.lblHistoryRule);
            this.pnlHistory.Controls.Add(this.lstSyncHistory);

            // 
            // 7. flpTasks (Lista de Cards de Tarefas)
            // 
            this.flpTasks.AutoScroll = true;
            this.flpTasks.BackColor = bgMain;
            this.flpTasks.Dock = DockStyle.Fill;
            this.flpTasks.FlowDirection = FlowDirection.TopDown;
            this.flpTasks.Padding = new Padding(12, 8, 12, 8);
            this.flpTasks.WrapContents = false;

            // 
            // tmrStopwatch & notifyIcon
            // 
            this.tmrStopwatch.Interval = 1000;
            this.tmrStopwatch.Tick += this.TmrStopwatch_Tick;

            this.notifyIcon.Icon = SystemIcons.Application;
            this.notifyIcon.Text = "Jira Focus Timer — Inativo";
            this.notifyIcon.Visible = true;
            this.notifyIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.Activate();
            };

            // Ordem exata de Dock no Form
            this.Controls.Add(this.flpTasks);
            this.Controls.Add(this.pnlFilters);
            this.Controls.Add(this.pnlActiveTimer);
            this.Controls.Add(this.pnlConfig);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.pnlHistory);
            this.Controls.Add(this.pnlFooter);

            this.Resize += (s, e) => RecalculateResponsivePositions();
            this.ResumeLayout(false);
            RecalculateResponsivePositions();
        }

        private static void StyleSecondaryButton(Button btn, string text, Size size, Color bg, Color border, Color fg)
        {
            btn.BackColor = bg;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = border;
            btn.FlatAppearance.BorderSize = 1;
            btn.Font = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold);
            btn.ForeColor = fg;
            btn.Size = size;
            btn.Text = text;
            btn.Cursor = Cursors.Hand;
        }

        private static void CreateFlatTextBox(Panel wrapper, TextBox txt, Point location, Size size, Color bg, Color border, Color fg)
        {
            wrapper.BackColor = bg;
            wrapper.Location = location;
            wrapper.Size = size;
            wrapper.Padding = new Padding(6, 4, 6, 4);
            wrapper.Paint += (s, e) =>
            {
                using (var pen = new Pen(border, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, wrapper.Width - 1, wrapper.Height - 1);
                }
            };

            txt.BackColor = bg;
            txt.BorderStyle = BorderStyle.None;
            txt.ForeColor = fg;
            txt.Font = new Font("Segoe UI", 8.75F);
            txt.Dock = DockStyle.Fill;

            wrapper.Controls.Add(txt);
        }

        private static void DrawBottomBorder(Graphics g, Control ctrl, Color color)
        {
            using (var pen = new Pen(color, 1))
            {
                g.DrawLine(pen, 0, ctrl.Height - 1, ctrl.Width, ctrl.Height - 1);
            }
        }

        /// <summary>
        /// Posiciona todos os botões e campos da direita matematicamente com base na largura real do Form,
        /// garantindo que nenhum botão suma ou corte em qualquer resolução/DPI.
        /// </summary>
        private void RecalculateResponsivePositions()
        {
            int w = this.ClientSize.Width;
            int rightMargin = 14;

            // 1. Header buttons (Topo | Mini | Conta)
            btnToggleConfig.Location = new Point(w - rightMargin - btnToggleConfig.Width, 14);
            btnToggleMini.Location = new Point(btnToggleConfig.Left - 6 - btnToggleMini.Width, 14);
            btnTopMost.Location = new Point(btnToggleMini.Left - 6 - btnTopMost.Width, 14);

            // 2. Painel de Configuração
            int configInputWidth = Math.Max(200, w - 92 - rightMargin);
            pnlUrlWrap.Width = configInputWidth;
            pnlEmailWrap.Width = configInputWidth;
            pnlTokenWrap.Width = configInputWidth;
            btnSaveConnect.Width = configInputWidth;

            // 3. Painel do Cronômetro Ativo
            lblTimerClock.Location = new Point(w - rightMargin - lblTimerClock.Width, 8);
            btnStopActive.Location = new Point(w - rightMargin - btnStopActive.Width, 42);
            int activeTextWidth = Math.Max(180, btnStopActive.Left - 24);
            lblActiveTaskKey.Width = activeTextWidth;
            lblActiveTaskTitle.Width = activeTextWidth;
            pnlActiveCommentWrap.Width = Math.Max(200, w - 92 - rightMargin);

            // 4. Painel de Abas e Filtros
            btnPinNewCard.Location = new Point(w - rightMargin - btnPinNewCard.Width, 10);
            int availableTabsWidth = btnPinNewCard.Left - 14 - 6;
            int tabMineWidth = (int)(availableTabsWidth * 0.46);
            int tabCerWidth = availableTabsWidth - tabMineWidth - 6;
            btnTabMyTasks.Location = new Point(14, 10);
            btnTabMyTasks.Width = tabMineWidth;
            btnTabCeremonies.Location = new Point(btnTabMyTasks.Right + 6, 10);
            btnTabCeremonies.Width = tabCerWidth;

            cmbProjects.Width = Math.Max(200, w - 102 - rightMargin);
            btnRefresh.Location = new Point(w - rightMargin - btnRefresh.Width, 80);
            pnlSearchWrap.Width = Math.Max(180, btnRefresh.Left - 8 - 14);

            // 5. Painel de Histórico
            lblHistoryRule.Location = new Point(w - rightMargin - lblHistoryRule.Width, 7);
            lstSyncHistory.Width = Math.Max(240, w - 28);

            // 6. Largura dos Cards na Lista (descontando barra de rolagem vertical para nunca gerar barra horizontal)
            if (flpTasks.Controls.Count > 0)
            {
                flpTasks.SuspendLayout();
                int cardWidth = GetOptimalCardWidth();
                foreach (Control c in flpTasks.Controls)
                {
                    c.Width = cardWidth;
                }
                flpTasks.ResumeLayout();
            }
        }

        private int GetOptimalCardWidth()
        {
            int scrollSpace = SystemInformation.VerticalScrollBarWidth + 28;
            return Math.Max(360, this.ClientSize.Width - scrollSpace);
        }
    }
}
