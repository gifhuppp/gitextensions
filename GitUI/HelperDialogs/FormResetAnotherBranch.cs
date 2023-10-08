using GitCommands.Git;
using GitExtUtils.GitUI;
using GitExtUtils.GitUI.Theming;
using GitUIPluginInterfaces;
using ResourceManager;

namespace GitUI.HelperDialogs
{
    public partial class FormResetAnotherBranch : GitModuleForm
    {
        private IGitRef[]? _localGitRefs;
        private readonly GitRevision _revision;
        private readonly TranslationString _localRefInvalid = new("The entered value '{0}' is not the name of an existing local branch.");

        public static FormResetAnotherBranch Create(GitUICommands commands, GitRevision revision)
            => new(commands, revision ?? throw new NotSupportedException(TranslatedStrings.NoRevision));

        private FormResetAnotherBranch(GitUICommands commands, GitRevision revision)
            : base(commands)
        {
            _revision = revision;

            InitializeComponent();

            pictureBox1.Image = DpiUtil.Scale(pictureBox1.Image);
            labelResetBranchWarning.AutoSize = true;
            labelResetBranchWarning.Dock = DockStyle.Fill;

            Height = tableLayoutPanel1.Height + tableLayoutPanel1.Top;
            tableLayoutPanel1.Dock = DockStyle.Fill;

            ActiveControl = Branches;

            InitializeComplete();

            labelResetBranchWarning.SetForeColorForBackColor();
        }

        private void Application_Idle(object sender, EventArgs e)
        {
            Application.Idle -= Application_Idle;

            if (Branches.Text.Length == 0)
            {
                Branches.DroppedDown = true;
            }
        }

        private IGitRef[] GetLocalBranchesWithoutCurrent()
        {
            var currentBranch = Module.GetSelectedBranch();
            var isDetachedHead = currentBranch == DetachedHeadParser.DetachedBranch;

            var selectedRevisionRemotes = _revision.Refs.Where(r => r.IsRemote).ToList();

            return Module.GetRefs(RefsFilter.Heads)
                .Where(r => r.IsHead)
                .Where(r => isDetachedHead || r.LocalName != currentBranch)
                .OrderByDescending(r => selectedRevisionRemotes.Any(r.IsTrackingRemote)) // Put local branches that track these remotes first
                .ToArray();
        }

        private void FormResetAnotherBranch_Load(object sender, EventArgs e)
        {
            _localGitRefs = GetLocalBranchesWithoutCurrent();

            Branches.DisplayMember = nameof(IGitRef.Name);
            Branches.Items.AddRange(_localGitRefs);

            commitSummaryUserControl.Revision = _revision;

            Application.Idle += Application_Idle;
        }

        private void Ok_Click(object sender, EventArgs e)
        {
            var gitRefToReset = _localGitRefs.FirstOrDefault(b => b.Name == Branches.Text);
            if (gitRefToReset is null)
            {
                MessageBox.Show(string.Format(_localRefInvalid.Text, Branches.Text), TranslatedStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var command = Commands.PushLocal(gitRefToReset.CompleteName, _revision.ObjectId, Module.GetGitExecPath(Module.WorkingDir), ForceReset.Checked);
            bool success = FormProcess.ShowDialog(this, UICommands, arguments: command, Module.WorkingDir, input: null, useDialogSettings: true);
            if (success)
            {
                UICommands.RepoChangedNotifier.Notify();
                Close();
            }
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
