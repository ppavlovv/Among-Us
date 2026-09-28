using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using KV3Bridge;

namespace AmongUsExternal
{
    public partial class OverlayForm
    {
        private SandboxBridgeClient sandboxBridge;
        private TabPage sandboxTab;
        private Label sandboxStatus;
        private ListBox sandboxTargets;
        private CheckBox sandboxNoClip;
        private CheckBox sandboxProtection;
        private CheckBox sandboxOrbit;
        private readonly List<Control> sandboxActions = new List<Control>();
        private DateTime nextSandboxPoll;

        private void BuildSandboxTab()
        {
            sandboxBridge = new SandboxBridgeClient(memory);
            sandboxTab = new TabPage("KV3");
            sandboxTab.BackColor = Color.FromArgb(25, 27, 34);
            sandboxTab.ForeColor = Color.White;
            var content = new Panel { Dock = DockStyle.Fill, AutoScroll = true,
                AutoScrollMinSize = new Size(0, 465), BackColor = sandboxTab.BackColor };
            sandboxTab.Controls.Add(content);

            sandboxStatus = new Label { Location = new Point(12, 8), Size = new Size(355, 34),
                Text = "KV3 baglantisi bekleniyor", ForeColor = Color.LightGoldenrodYellow };
            content.Controls.Add(sandboxStatus);

            sandboxNoClip = AddSandboxToggle(content, "No-clip", 48, BridgeCommand.NoClip);
            sandboxProtection = AddSandboxToggle(content, "Olum / surgun korumasi", 76,
                BridgeCommand.Protection);
            sandboxOrbit = AddSandboxToggle(content, "Donen halka", 104, BridgeCommand.Orbit);

            sandboxTargets = new ListBox { Location = new Point(12, 136), Size = new Size(355, 96),
                BackColor = Color.FromArgb(15, 17, 22), ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 9F) };
            content.Controls.Add(sandboxTargets);

            AddSandboxButton(content, "Secileni oldur", 12, 241, 172, delegate {
                byte id;
                if (TrySandboxTarget(out id)) SendSandbox(BridgeCommand.KillSelected, id);
            });
            AddSandboxButton(content, "Yanina cek", 193, 241, 174, delegate {
                byte id;
                if (TrySandboxTarget(out id)) SendSandbox(BridgeCommand.PullSelected, id);
            });
            AddSandboxButton(content, "Herkesi yanina cek", 12, 275, 172,
                delegate { SendSandbox(BridgeCommand.PullAll, 0); });
            AddSandboxButton(content, "Herkesi dirilt", 193, 275, 174,
                delegate { SendSandbox(BridgeCommand.ReviveAll, 0); });
            AddSandboxButton(content, "Konumlari karistir", 12, 309, 172,
                delegate { SendSandbox(BridgeCommand.Shuffle, 0); });
            AddSandboxButton(content, "Sirayi iptal et", 193, 309, 174,
                delegate { SendSandbox(BridgeCommand.Cancel, 0); });
            AddSandboxButton(content, "Tumunu oldur", 12, 343, 172, delegate {
                if (MessageBox.Show(this, "Freeplay'deki tum canli deneme oyunculari oldurulsun mu?",
                    "Toplu oldurme", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    SendSandbox(BridgeCommand.KillAll, 0);
            });
            AddSandboxButton(content, "Kendini dirilt", 193, 343, 174,
                delegate { SendSandbox(BridgeCommand.ReviveSelf, 0); });
            AddSandboxButton(content, "Olum / koruma testi", 12, 377, 355, delegate {
                if (MessageBox.Show(this, "Koruma kapaliysa karakterin olur. Test edilsin mi?",
                    "Yerel test", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    SendSandbox(BridgeCommand.TestDeath, 0);
            });

            tabControl.TabPages.Add(sandboxTab);
            tabControl.SelectedIndexChanged += delegate { PollSandbox(true); };
        }

        private CheckBox AddSandboxToggle(Control parent, string label, int y, BridgeCommand command)
        {
            var box = new CheckBox { Text = label, Location = new Point(12, y),
                Size = new Size(355, 25), ForeColor = Color.White };
            box.CheckedChanged += delegate {
                if (!syncingCheckboxState) SendSandbox(command, box.Checked ? (byte)1 : (byte)0);
            };
            parent.Controls.Add(box);
            sandboxActions.Add(box);
            return box;
        }

        private void AddSandboxButton(Control parent, string label, int x, int y, int width,
            Action action)
        {
            var button = new Button { Text = label, Location = new Point(x, y),
                Size = new Size(width, 29), FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(44, 73, 91), ForeColor = Color.White };
            button.Click += delegate { action(); };
            parent.Controls.Add(button);
            sandboxActions.Add(button);
        }

        private bool TrySandboxTarget(out byte id)
        {
            var target = sandboxTargets.SelectedItem as BridgeTarget;
            id = target == null ? (byte)0 : target.Id;
            if (target != null) return true;
            sandboxStatus.Text = "Once hedef sec";
            return false;
        }

        private void SendSandbox(BridgeCommand command, byte argument)
        {
            ApplySandboxSnapshot(sandboxBridge.Send(command, argument));
        }

        private void PollSandbox(bool force = false)
        {
            if (sandboxTab == null || tabControl.SelectedTab != sandboxTab) return;
            if (!force && DateTime.UtcNow < nextSandboxPoll) return;
            nextSandboxPoll = DateTime.UtcNow.AddSeconds(1);
            SendSandbox(BridgeCommand.Snapshot, 0);
        }

        private void ApplySandboxSnapshot(BridgeSnapshot snapshot)
        {
            sandboxStatus.Text = snapshot.Connected
                ? (snapshot.Freeplay ? "Freeplay: " : "Online: ") + snapshot.Status
                : snapshot.Status;
            bool enabled = snapshot.Connected && snapshot.Freeplay;
            foreach (Control control in sandboxActions) control.Enabled = enabled;
            sandboxNoClip.Enabled = snapshot.Connected && snapshot.NoClipAvailable;
            SetCheckboxState(sandboxNoClip, snapshot.NoClip);
            SetCheckboxState(sandboxProtection, snapshot.Protection);
            SetCheckboxState(sandboxOrbit, snapshot.Orbit);

            var oldTarget = sandboxTargets.SelectedItem as BridgeTarget;
            byte? oldId = oldTarget == null ? (byte?)null : oldTarget.Id;
            sandboxTargets.BeginUpdate();
            try
            {
                sandboxTargets.Items.Clear();
                foreach (BridgeTarget target in snapshot.Targets)
                {
                    int index = sandboxTargets.Items.Add(target);
                    if (oldId.HasValue && oldId.Value == target.Id)
                        sandboxTargets.SelectedIndex = index;
                }
            }
            finally { sandboxTargets.EndUpdate(); }
        }
    }
}
