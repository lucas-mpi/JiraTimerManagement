using System.Drawing;
using System.Windows.Forms;

namespace JiraTimerManagement
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        // Controles principais
        private Panel pnlHeader;
        private Label lblAppTitle;
        private Label lblUserStatus;
        private CheckBox chkTopMost;
        private Button btnToggleMini;
        private Button btnToggleConfig;

        // Painel retrátil de Login / Configuração Jira
        private Panel pnlConfig;
        private Label lblUrl;
        private TextBox txtBaseUrl;
        private Label lblEmail;
        private TextBox txtEmail;
        private Label lblToken;
        private TextBox txtApiToken;
        private CheckBox chkSimulationMode;
        private Button btnSaveConnect;

        // Painel do Cronômetro Ativo
        private Panel pnlActiveTimer;
        private Label lblActiveBadge;
        private Label lblActiveTaskKey;
        private Label lblActiveTaskTitle;
        private Label lblTimerClock;
        private Button btnStopActive;

        // Painel de Filtros (Seleção de Projeto + Busca)
        private Panel pnlFilters;
        private Label lblProjectFilter;
        private ComboBox cmbProjects;
        private TextBox txtSearch;
        private Button btnRefresh;

        // Lista de Cards de Tarefas
        private FlowLayoutPanel flpTasks;

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
            this.chkTopMost = new CheckBox();
            this.btnToggleMini = new Button();
            this.btnToggleConfig = new Button();

            this.pnlConfig = new Panel();
            this.lblUrl = new Label();
            this.txtBaseUrl = new TextBox();
            this.lblEmail = new Label();
            this.txtEmail = new TextBox();
            this.lblToken = new Label();
            this.txtApiToken = new TextBox();
            this.chkSimulationMode = new CheckBox();
            this.btnSaveConnect = new Button();

            this.pnlActiveTimer = new Panel();
            this.lblActiveBadge = new Label();
            this.lblActiveTaskKey = new Label();
            this.lblActiveTaskTitle = new Label();
            this.lblTimerClock = new Label();
            this.btnStopActive = new Button();

            this.pnlFilters = new Panel();
            this.lblProjectFilter = new Label();
            this.cmbProjects = new ComboBox();
            this.txtSearch = new TextBox();
            this.btnRefresh = new Button();

            this.flpTasks = new FlowLayoutPanel();

            this.pnlFooter = new Panel();
            this.lblSyncStatus = new Label();

            this.tmrStopwatch = new Timer(this.components);
            this.notifyIcon = new NotifyIcon(this.components);

            this.SuspendLayout();

            // Cores do Tema Minimalista Escuro
            Color bgMain = Color.FromArgb(18, 22, 31);
            Color bgHeader = Color.FromArgb(25, 31, 44);
            Color bgCard = Color.FromArgb(30, 38, 54);
            Color bgInput = Color.FromArgb(15, 19, 27);
            Color fgPrimary = Color.FromArgb(241, 245, 249);
            Color fgMuted = Color.FromArgb(148, 163, 184);
            Color accentBlue = Color.FromArgb(56, 189, 248);

            // 
            // Form1
            // 
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = bgMain;
            this.ClientSize = new Size(460, 680);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.ForeColor = fgPrimary;
            this.MinimumSize = new Size(440, 210);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Jira Focus Timer — Apontamento Rápido";

            // 
            // pnlHeader (Topo)
            // 
            this.pnlHeader.BackColor = bgHeader;
            this.pnlHeader.Dock = DockStyle.Top;
            this.pnlHeader.Height = 54;
            this.pnlHeader.Padding = new Padding(12, 8, 12, 8);

            this.lblAppTitle.AutoSize = true;
            this.lblAppTitle.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            this.lblAppTitle.ForeColor = accentBlue;
            this.lblAppTitle.Location = new Point(12, 7);
            this.lblAppTitle.Text = "⏱ Jira Focus Timer";

            this.lblUserStatus.AutoSize = true;
            this.lblUserStatus.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular);
            this.lblUserStatus.ForeColor = fgMuted;
            this.lblUserStatus.Location = new Point(14, 29);
            this.lblUserStatus.Text = "Conectando...";

            this.chkTopMost.AutoSize = true;
            this.chkTopMost.Font = new Font("Segoe UI", 8.25F);
            this.chkTopMost.ForeColor = fgMuted;
            this.chkTopMost.Location = new Point(235, 18);
            this.chkTopMost.Text = "📌 Topo";
            this.chkTopMost.Cursor = Cursors.Hand;
            this.chkTopMost.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.chkTopMost.CheckedChanged += this.ChkTopMost_CheckedChanged;

            this.btnToggleMini.FlatStyle = FlatStyle.Flat;
            this.btnToggleMini.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            this.btnToggleMini.Font = new Font("Segoe UI", 8.25F);
            this.btnToggleMini.ForeColor = fgPrimary;
            this.btnToggleMini.Location = new Point(305, 13);
            this.btnToggleMini.Size = new Size(64, 28);
            this.btnToggleMini.Text = "🗕 Mini";
            this.btnToggleMini.Cursor = Cursors.Hand;
            this.btnToggleMini.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnToggleMini.Click += this.BtnToggleMini_Click;

            this.btnToggleConfig.FlatStyle = FlatStyle.Flat;
            this.btnToggleConfig.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            this.btnToggleConfig.Font = new Font("Segoe UI", 8.25F);
            this.btnToggleConfig.ForeColor = fgPrimary;
            this.btnToggleConfig.Location = new Point(375, 13);
            this.btnToggleConfig.Size = new Size(73, 28);
            this.btnToggleConfig.Text = "⚙ Conta";
            this.btnToggleConfig.Cursor = Cursors.Hand;
            this.btnToggleConfig.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnToggleConfig.Click += this.BtnToggleConfig_Click;

            this.pnlHeader.Controls.Add(this.lblAppTitle);
            this.pnlHeader.Controls.Add(this.lblUserStatus);
            this.pnlHeader.Controls.Add(this.chkTopMost);
            this.pnlHeader.Controls.Add(this.btnToggleMini);
            this.pnlHeader.Controls.Add(this.btnToggleConfig);

            // 
            // pnlConfig (Login / Conta Jira retrátil)
            // 
            this.pnlConfig.BackColor = Color.FromArgb(23, 29, 42);
            this.pnlConfig.Dock = DockStyle.Top;
            this.pnlConfig.Height = 168;
            this.pnlConfig.Visible = false;
            this.pnlConfig.Padding = new Padding(12);

            this.chkSimulationMode.AutoSize = true;
            this.chkSimulationMode.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            this.chkSimulationMode.ForeColor = Color.FromArgb(250, 204, 21);
            this.chkSimulationMode.Location = new Point(14, 8);
            this.chkSimulationMode.Text = "Modo Protótipo (Dados Simulados para Teste Rápido)";
            this.chkSimulationMode.CheckedChanged += this.ChkSimulationMode_CheckedChanged;

            this.lblUrl.AutoSize = true;
            this.lblUrl.ForeColor = fgMuted;
            this.lblUrl.Location = new Point(12, 34);
            this.lblUrl.Text = "URL Jira:";

            this.txtBaseUrl.BackColor = bgInput;
            this.txtBaseUrl.BorderStyle = BorderStyle.FixedSingle;
            this.txtBaseUrl.ForeColor = fgPrimary;
            this.txtBaseUrl.Location = new Point(90, 31);
            this.txtBaseUrl.Size = new Size(354, 23);
            this.txtBaseUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            this.lblEmail.AutoSize = true;
            this.lblEmail.ForeColor = fgMuted;
            this.lblEmail.Location = new Point(12, 64);
            this.lblEmail.Text = "Seu E-mail:";

            this.txtEmail.BackColor = bgInput;
            this.txtEmail.BorderStyle = BorderStyle.FixedSingle;
            this.txtEmail.ForeColor = fgPrimary;
            this.txtEmail.Location = new Point(90, 61);
            this.txtEmail.Size = new Size(354, 23);
            this.txtEmail.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            this.lblToken.AutoSize = true;
            this.lblToken.ForeColor = fgMuted;
            this.lblToken.Location = new Point(12, 94);
            this.lblToken.Text = "API Token:";

            this.txtApiToken.BackColor = bgInput;
            this.txtApiToken.BorderStyle = BorderStyle.FixedSingle;
            this.txtApiToken.ForeColor = fgPrimary;
            this.txtApiToken.Location = new Point(90, 91);
            this.txtApiToken.PasswordChar = '•';
            this.txtApiToken.Size = new Size(354, 23);
            this.txtApiToken.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            this.btnSaveConnect.BackColor = Color.FromArgb(37, 99, 235);
            this.btnSaveConnect.FlatStyle = FlatStyle.Flat;
            this.btnSaveConnect.FlatAppearance.BorderSize = 0;
            this.btnSaveConnect.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            this.btnSaveConnect.ForeColor = Color.White;
            this.btnSaveConnect.Location = new Point(90, 124);
            this.btnSaveConnect.Size = new Size(354, 32);
            this.btnSaveConnect.Text = "Salvar e Carregar Tarefas da Conta";
            this.btnSaveConnect.Cursor = Cursors.Hand;
            this.btnSaveConnect.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.btnSaveConnect.Click += this.BtnSaveConnect_Click;

            this.pnlConfig.Controls.Add(this.chkSimulationMode);
            this.pnlConfig.Controls.Add(this.lblUrl);
            this.pnlConfig.Controls.Add(this.txtBaseUrl);
            this.pnlConfig.Controls.Add(this.lblEmail);
            this.pnlConfig.Controls.Add(this.txtEmail);
            this.pnlConfig.Controls.Add(this.lblToken);
            this.pnlConfig.Controls.Add(this.txtApiToken);
            this.pnlConfig.Controls.Add(this.btnSaveConnect);

            // 
            // pnlActiveTimer (Painel do Cronômetro Ativo)
            // 
            this.pnlActiveTimer.BackColor = bgCard;
            this.pnlActiveTimer.Dock = DockStyle.Top;
            this.pnlActiveTimer.Height = 104;
            this.pnlActiveTimer.Padding = new Padding(14, 10, 14, 10);

            this.lblActiveBadge.AutoSize = true;
            this.lblActiveBadge.Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold);
            this.lblActiveBadge.ForeColor = fgMuted;
            this.lblActiveBadge.Location = new Point(14, 10);
            this.lblActiveBadge.Text = "⏸ NENHUMA TAREFA EM CONTAGEM";

            this.lblActiveTaskKey.AutoSize = true;
            this.lblActiveTaskKey.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            this.lblActiveTaskKey.ForeColor = fgPrimary;
            this.lblActiveTaskKey.Location = new Point(14, 30);
            this.lblActiveTaskKey.Text = "Selecione uma tarefa abaixo";

            this.lblActiveTaskTitle.AutoEllipsis = true;
            this.lblActiveTaskTitle.Font = new Font("Segoe UI", 8.5F);
            this.lblActiveTaskTitle.ForeColor = fgMuted;
            this.lblActiveTaskTitle.Location = new Point(14, 54);
            this.lblActiveTaskTitle.Size = new Size(275, 36);
            this.lblActiveTaskTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.lblActiveTaskTitle.Text = "Ao clicar em ▶ Iniciar em outra atividade, a anterior é interrompida e sincronizada automaticamente.";

            this.lblTimerClock.Font = new Font("Consolas", 18F, FontStyle.Bold);
            this.lblTimerClock.ForeColor = fgMuted;
            this.lblTimerClock.Location = new Point(300, 12);
            this.lblTimerClock.Size = new Size(146, 34);
            this.lblTimerClock.TextAlign = ContentAlignment.MiddleRight;
            this.lblTimerClock.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.lblTimerClock.Text = "00:00:00";

            this.btnStopActive.BackColor = Color.FromArgb(220, 38, 38);
            this.btnStopActive.Enabled = false;
            this.btnStopActive.FlatStyle = FlatStyle.Flat;
            this.btnStopActive.FlatAppearance.BorderSize = 0;
            this.btnStopActive.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            this.btnStopActive.ForeColor = Color.White;
            this.btnStopActive.Location = new Point(312, 54);
            this.btnStopActive.Size = new Size(134, 34);
            this.btnStopActive.Text = "⏹ Parar & Sincronizar";
            this.btnStopActive.Cursor = Cursors.Hand;
            this.btnStopActive.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnStopActive.Click += this.BtnStopActive_Click;

            this.pnlActiveTimer.Controls.Add(this.lblActiveBadge);
            this.pnlActiveTimer.Controls.Add(this.lblActiveTaskKey);
            this.pnlActiveTimer.Controls.Add(this.lblActiveTaskTitle);
            this.pnlActiveTimer.Controls.Add(this.lblTimerClock);
            this.pnlActiveTimer.Controls.Add(this.btnStopActive);

            // 
            // pnlFilters (Seletor de Projeto Multi-Time e Busca)
            // 
            this.pnlFilters.BackColor = bgMain;
            this.pnlFilters.Dock = DockStyle.Top;
            this.pnlFilters.Height = 76;
            this.pnlFilters.Padding = new Padding(12, 8, 12, 6);

            this.lblProjectFilter.AutoSize = true;
            this.lblProjectFilter.Font = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold);
            this.lblProjectFilter.ForeColor = fgMuted;
            this.lblProjectFilter.Location = new Point(12, 10);
            this.lblProjectFilter.Text = "Projeto / Time:";

            this.cmbProjects.BackColor = bgCard;
            this.cmbProjects.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbProjects.FlatStyle = FlatStyle.Flat;
            this.cmbProjects.ForeColor = fgPrimary;
            this.cmbProjects.Location = new Point(102, 7);
            this.cmbProjects.Size = new Size(346, 23);
            this.cmbProjects.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.cmbProjects.SelectedIndexChanged += this.CmbProjects_SelectedIndexChanged;

            this.txtSearch.BackColor = bgCard;
            this.txtSearch.BorderStyle = BorderStyle.FixedSingle;
            this.txtSearch.ForeColor = fgPrimary;
            this.txtSearch.Location = new Point(12, 40);
            this.txtSearch.Size = new Size(348, 25);
            this.txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.txtSearch.TextChanged += this.TxtSearch_TextChanged;

            this.btnRefresh.BackColor = Color.FromArgb(40, 50, 70);
            this.btnRefresh.FlatStyle = FlatStyle.Flat;
            this.btnRefresh.FlatAppearance.BorderSize = 0;
            this.btnRefresh.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            this.btnRefresh.ForeColor = fgPrimary;
            this.btnRefresh.Location = new Point(368, 39);
            this.btnRefresh.Size = new Size(80, 26);
            this.btnRefresh.Text = "↻ Atualizar";
            this.btnRefresh.Cursor = Cursors.Hand;
            this.btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnRefresh.Click += this.BtnRefresh_Click;

            this.pnlFilters.Controls.Add(this.lblProjectFilter);
            this.pnlFilters.Controls.Add(this.cmbProjects);
            this.pnlFilters.Controls.Add(this.txtSearch);
            this.pnlFilters.Controls.Add(this.btnRefresh);

            // 
            // pnlFooter (Rodapé de Status de Sincronização)
            // 
            this.pnlFooter.BackColor = bgHeader;
            this.pnlFooter.Dock = DockStyle.Bottom;
            this.pnlFooter.Height = 34;
            this.pnlFooter.Padding = new Padding(12, 6, 12, 6);

            this.lblSyncStatus.AutoEllipsis = true;
            this.lblSyncStatus.Dock = DockStyle.Fill;
            this.lblSyncStatus.Font = new Font("Segoe UI", 8.25F);
            this.lblSyncStatus.ForeColor = Color.FromArgb(52, 211, 153);
            this.lblSyncStatus.TextAlign = ContentAlignment.MiddleLeft;
            this.lblSyncStatus.Text = "✓ Pronto. Escolha uma tarefa e clique em ▶ Iniciar.";

            this.pnlFooter.Controls.Add(this.lblSyncStatus);

            // 
            // flpTasks (Lista de Tarefas)
            // 
            this.flpTasks.AutoScroll = true;
            this.flpTasks.BackColor = bgMain;
            this.flpTasks.Dock = DockStyle.Fill;
            this.flpTasks.FlowDirection = FlowDirection.TopDown;
            this.flpTasks.Padding = new Padding(10, 6, 10, 10);
            this.flpTasks.WrapContents = false;
            this.flpTasks.SizeChanged += this.FlpTasks_SizeChanged;

            // 
            // tmrStopwatch
            // 
            this.tmrStopwatch.Interval = 1000;
            this.tmrStopwatch.Tick += this.TmrStopwatch_Tick;

            // 
            // notifyIcon
            // 
            this.notifyIcon.Icon = SystemIcons.Application;
            this.notifyIcon.Text = "Jira Focus Timer — Inativo";
            this.notifyIcon.Visible = true;
            this.notifyIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.Activate();
            };

            // Ordem de acoplamento (Dock)
            this.Controls.Add(this.flpTasks);
            this.Controls.Add(this.pnlFilters);
            this.Controls.Add(this.pnlActiveTimer);
            this.Controls.Add(this.pnlConfig);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.pnlFooter);

            this.ResumeLayout(false);
        }
    }
}
