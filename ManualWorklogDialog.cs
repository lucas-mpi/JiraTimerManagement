using System;
using System.Drawing;
using System.Windows.Forms;

namespace JiraTimerManagement
{
    /// <summary>
    /// Modal minimalista para Lançamento Manual de Horas + Comentário em qualquer card (atribuído ou Cerimônia).
    /// </summary>
    public class ManualWorklogDialog : Form
    {
        private TextBox txtDuration;
        private TextBox txtComment;

        public int EnteredSeconds { get; private set; }
        public string EnteredComment { get; private set; }

        public ManualWorklogDialog(JiraTaskItem task)
        {
            Color bgMain = Color.FromArgb(18, 22, 31);
            Color bgCard = Color.FromArgb(30, 38, 54);
            Color bgInput = Color.FromArgb(15, 19, 27);
            Color fgPrimary = Color.FromArgb(241, 245, 249);
            Color fgMuted = Color.FromArgb(148, 163, 184);
            Color accentBlue = Color.FromArgb(56, 189, 248);

            this.Text = $"Lançar Horas Manuais / Comentário — {task.Key}";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new Size(410, 310);
            this.BackColor = bgMain;
            this.ForeColor = fgPrimary;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            var lblHeaderKey = new Label
            {
                AutoSize = true,
                Location = new Point(16, 14),
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                ForeColor = accentBlue,
                Text = $"{task.Key} • {task.Status}"
            };

            var lblHeaderSummary = new Label
            {
                AutoEllipsis = true,
                Location = new Point(16, 36),
                Size = new Size(378, 20),
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = fgMuted,
                Text = task.Summary
            };

            var lblPresets = new Label
            {
                AutoSize = true,
                Location = new Point(16, 66),
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                ForeColor = fgPrimary,
                Text = "⏱ Tempo gasto (clique em um atalho ou digite ex: 15m, 1h 30m):"
            };

            // Botões de atalho rápido (Daily 15m, Chamada 30m, etc.)
            string[] presets = { "15m", "30m", "45m", "1h", "1h 30m", "2h" };
            int btnX = 16;
            foreach (string preset in presets)
            {
                var btnChip = new Button
                {
                    Text = "+" + preset,
                    Location = new Point(btnX, 88),
                    Size = new Size(58, 26),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = bgCard,
                    ForeColor = accentBlue,
                    Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnChip.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
                string captured = preset;
                btnChip.Click += (s, e) => { txtDuration.Text = captured; };
                this.Controls.Add(btnChip);
                btnX += 63;
            }

            txtDuration = new TextBox
            {
                Location = new Point(16, 122),
                Size = new Size(378, 25),
                BackColor = bgInput,
                ForeColor = fgPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Text = "15m"
            };

            var lblComment = new Label
            {
                AutoSize = true,
                Location = new Point(16, 158),
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                ForeColor = fgPrimary,
                Text = "💬 Comentário / Descrição da atividade ou cerimônia:"
            };

            txtComment = new TextBox
            {
                Location = new Point(16, 180),
                Size = new Size(378, 66),
                Multiline = true,
                BackColor = bgInput,
                ForeColor = fgPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F),
                Text = task.IsCeremonyOrShared
                    ? "Participação na cerimônia / alinhamento técnico da equipe"
                    : ""
            };

            var btnCancel = new Button
            {
                Text = "Cancelar",
                Location = new Point(196, 260),
                Size = new Size(92, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = bgCard,
                ForeColor = fgMuted,
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            var btnConfirm = new Button
            {
                Text = "✓ Lançar no Jira",
                Location = new Point(296, 260),
                Size = new Size(98, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += BtnConfirm_Click;

            this.Controls.Add(lblHeaderKey);
            this.Controls.Add(lblHeaderSummary);
            this.Controls.Add(lblPresets);
            this.Controls.Add(txtDuration);
            this.Controls.Add(lblComment);
            this.Controls.Add(txtComment);
            this.Controls.Add(btnCancel);
            this.Controls.Add(btnConfirm);

            this.CancelButton = btnCancel;
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            int seconds = JiraService.ParseDurationToSeconds(txtDuration.Text);
            if (seconds <= 0)
            {
                MessageBox.Show(
                    "Informe um tempo válido (exemplos: 15m, 30m, 1h 30m ou 45).",
                    "Tempo inválido",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            EnteredSeconds = seconds;
            EnteredComment = txtComment.Text.Trim();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }

    /// <summary>
    /// Modal minimalista para buscar/fixar qualquer card do Jira pelo código (ex: NDSTRS-100) na aba de Cerimônias.
    /// </summary>
    public class PinCardDialog : Form
    {
        private TextBox txtKey;
        private TextBox txtTitle;

        public string IssueKey { get; private set; }
        public string CustomSummary { get; private set; }

        public PinCardDialog()
        {
            Color bgMain = Color.FromArgb(18, 22, 31);
            Color bgCard = Color.FromArgb(30, 38, 54);
            Color bgInput = Color.FromArgb(15, 19, 27);
            Color fgPrimary = Color.FromArgb(241, 245, 249);
            Color fgMuted = Color.FromArgb(148, 163, 184);

            this.Text = "⭐ Fixar Card de Cerimônia / Chamada";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new Size(380, 210);
            this.BackColor = bgMain;
            this.ForeColor = fgPrimary;
            this.Font = new Font("Segoe UI", 9F);

            var lblKey = new Label
            {
                AutoSize = true,
                Location = new Point(16, 16),
                Font = new Font("Segoe UI Semibold", 8.75F, FontStyle.Bold),
                Text = "Código do Card no Jira (ex: NDSTRS-100 ou SUP-15):"
            };

            txtKey = new TextBox
            {
                Location = new Point(16, 38),
                Size = new Size(348, 25),
                BackColor = bgInput,
                ForeColor = fgPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "NDSTRS-105"
            };

            var lblTitle = new Label
            {
                AutoSize = true,
                Location = new Point(16, 76),
                Font = new Font("Segoe UI", 8.25F),
                ForeColor = fgMuted,
                Text = "Descrição/apelido (usado no modo protótipo):"
            };

            txtTitle = new TextBox
            {
                Location = new Point(16, 98),
                Size = new Size(348, 25),
                BackColor = bgInput,
                ForeColor = fgPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "[Cerimônia] Alinhamento Técnico / Chamada"
            };

            var btnCancel = new Button
            {
                Text = "Cancelar",
                Location = new Point(170, 152),
                Size = new Size(90, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = bgCard,
                ForeColor = fgMuted,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            var btnAdd = new Button
            {
                Text = "⭐ Fixar Card",
                Location = new Point(268, 152),
                Size = new Size(96, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 8.75F, FontStyle.Bold)
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtKey.Text))
                {
                    MessageBox.Show("Informe o código do card (ex: NDSTRS-100).", "Atenção");
                    return;
                }
                IssueKey = txtKey.Text.Trim().ToUpperInvariant();
                CustomSummary = txtTitle.Text.Trim();
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            this.Controls.Add(lblKey);
            this.Controls.Add(txtKey);
            this.Controls.Add(lblTitle);
            this.Controls.Add(txtTitle);
            this.Controls.Add(btnCancel);
            this.Controls.Add(btnAdd);
            this.CancelButton = btnCancel;
        }
    }
}
