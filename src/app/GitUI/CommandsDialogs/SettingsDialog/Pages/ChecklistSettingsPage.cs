﻿#nullable enable

using GitCommands;
using GitCommands.Config;
using GitCommands.DiffMergeTools;
using GitCommands.Git;
using GitCommands.Utils;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Translations;
using GitExtUtils.GitUI.Theming;
using GitUI.CommandsDialogs.SettingsDialog.ShellExtension;
using Microsoft;
using ResourceManager;

namespace GitUI.CommandsDialogs.SettingsDialog.Pages
{
    public partial class ChecklistSettingsPage : SettingsPageWithHeader
    {
        #region Translations
        private readonly TranslationString _wrongGitVersion =
            new("Git found but version {0} is not supported. Upgrade to version {1} or later");

        private readonly TranslationString _notRecommendedGitVersion =
            new("Git found but version {0} is older than recommended. Upgrade to version {1} or later");

        private readonly TranslationString _gitVersionFound =
            new("Git {0} is found on your computer.");

        private readonly TranslationString _sshClientNotFound = new("SSH client not found: {0}.");

        private readonly TranslationString _otherSshClient = new("Other SSH client configured: {0}.");

        private readonly TranslationString _linuxToolsSshNotFound =
            new("Linux tools (sh) not found. To solve this problem you can set the correct path in settings.");

        private readonly TranslationString _solveGitCommandFailedCaption =
            new("Locate git");

        private readonly TranslationString _gitCanBeRun =
            new("Git can be run using: {0}");

        private readonly TranslationString _gitCanBeRunCaption =
            new("Locate git");

        private readonly TranslationString _solveGitCommandFailed =
            new("The command to run git could not be determined automatically." + Environment.NewLine +
                "Please make sure that Git for Windows is installed or set the correct command manually.");

        private readonly TranslationString _shellExtRegistered =
            new("Shell extensions registered properly.");

        private readonly TranslationString _shellExtNoInstalled =
            new("Shell extensions are not installed. Run the installer to install the shell extensions.");

        private readonly TranslationString _shellExtNeedsToBeRegistered =
            new("{0} needs to be registered in order to use the shell extensions.");

        private readonly TranslationString _registryKeyGitExtensionsMissing =
            new("Registry entry missing [Software\\GitExtensions\\InstallDir].");

        private readonly TranslationString _registryKeyGitExtensionsFaulty =
            new("Invalid installation directory stored in [Software\\GitExtensions\\InstallDir].");

        private readonly TranslationString _registryKeyGitExtensionsCorrect =
            new("Git Extensions is properly registered.");

        private readonly TranslationString _plinkputtyGenpageantNotFound =
            new("PuTTY is configured as SSH client but cannot find plink.exe, puttygen.exe or pageant.exe.");

        private readonly TranslationString _puttyConfigured =
            new("SSH client PuTTY is configured properly.");

        private readonly TranslationString _opensshUsed =
            new("Default SSH client, OpenSSH, will be used. (commandline window will appear on pull, push and clone operations)");

        private readonly TranslationString _languageConfigured =
            new("The configured language is {0}.");

        private readonly TranslationString _noLanguageConfigured =
            new("There is no language configured for Git Extensions.");

        private readonly TranslationString _noEmailSet =
            new("You need to configure a username and an email address.");

        private readonly TranslationString _emailSet =
            new("A username and an email address are configured.");

        private readonly TranslationString _mergeToolXConfiguredNeedsCmd =
            new("{0} is configured as mergetool, this is a custom mergetool and needs a custom cmd to be configured.");

        private readonly TranslationString _customMergeToolXConfigured =
            new("There is a custom mergetool configured: {0}");

        private readonly TranslationString _mergeToolXConfigured =
            new("There is a mergetool configured: {0}");

        private readonly TranslationString _linuxToolsSshFound =
            new("Linux tools (sh) found on your computer.");

        private readonly TranslationString _gitNotFound =
            new("Git not found. To solve this problem you can set the correct path in settings.");

        private readonly TranslationString _adviceDiffToolConfiguration =
            new("You should configure a diff tool to show file diff in external program.");

        private readonly TranslationString _diffToolXConfigured =
            new("There is a difftool configured: {0}");

        private readonly TranslationString _configureMergeTool =
            new("You need to configure merge tool in order to solve merge conflicts.");

        private readonly TranslationString _noDiffToolConfiguredCaption =
            new("Difftool");

        private readonly TranslationString _puttyFoundAuto =
            new("All paths needed for PuTTY could be automatically found and are set.");

        private readonly TranslationString _linuxToolsShNotFound =
            new("The path to linux tools (sh) could not be found automatically." + Environment.NewLine +
                "Please make sure there are linux tools installed (through Git for Windows or cygwin) or set the correct path manually.");

        private readonly TranslationString _linuxToolsShNotFoundCaption =
            new("Locate linux tools");

        private readonly TranslationString _shCanBeRun =
            new("Command sh can be run using: {0}sh");

        private readonly TranslationString _shCanBeRunCaption =
            new("Locate linux tools");

        private readonly TranslationString _gcmDetectedCaption = new("Obsolete git-credential-winstore.exe detected");
        #endregion

        private const string _putty = "PuTTY";
        private DiffMergeToolConfigurationManager? _diffMergeToolConfigurationManager;

        /// <summary>
        /// TODO: remove this direct dependency to another SettingsPage later when possible.
        /// </summary>
        public SshSettingsPage? SshSettingsPage { get; set; }

        public ChecklistSettingsPage(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            InitializeComponent();
            GcmDetected.FlatAppearance.MouseOverBackColor.AdaptBackColor();
            GitFound.FlatAppearance.MouseOverBackColor.AdaptBackColor();
            translationConfig.FlatAppearance.MouseOverBackColor.AdaptBackColor();
            SshConfig.FlatAppearance.MouseOverBackColor.AdaptBackColor();
            UserNameSet.FlatAppearance.MouseOverBackColor.AdaptBackColor();
            MergeTool.FlatAppearance.MouseOverBackColor.AdaptBackColor();
            GitExtensionsInstall.FlatAppearance.MouseOverBackColor.AdaptBackColor();
            GitBinFound.FlatAppearance.MouseOverBackColor.AdaptBackColor();
            DiffTool.FlatAppearance.MouseOverBackColor.AdaptBackColor();
            ShellExtensionsRegistered.FlatAppearance.MouseOverBackColor.AdaptBackColor();
            InitializeComplete();
        }

        public override bool IsInstantSavePage => true;

        public override void OnPageShown()
        {
            CheckSettings();
        }

        private string? GetGlobalSetting(string settingName)
        {
            return CommonLogic.GitConfigSettingsSet.GlobalSettings.GetValue(settingName);
        }

        private void translationConfig_Click(object sender, EventArgs e)
        {
            using (FormChooseTranslation frm = new())
            {
                frm.ShowDialog(this); // will set Settings.Translation
            }

            PageHost.LoadAll();

            Translator.Translate(this, AppSettings.CurrentTranslation);
            SaveAndRescan_Click(this, EventArgs.Empty);
        }

        private void SshConfig_Click(object sender, EventArgs e)
        {
            if (GitSshHelpers.IsPlink)
            {
                Validates.NotNull(SshSettingsPage);
                if (SshSettingsPage.AutoFindPuttyPaths())
                {
                    MessageBox.Show(this, _puttyFoundAuto.Text, _putty, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    PageHost.GotoPage(SshSettingsPage.GetPageReference());
                }
            }
            else
            {
                PageHost.GotoPage(SshSettingsPage.GetPageReference());
            }
        }

        private void GitExtensionsInstall_Click(object sender, EventArgs e)
        {
            CheckSettingsLogic.SolveGitExtensionsDir();
            CheckSettings();
        }

        private void GitBinFound_Click(object sender, EventArgs e)
        {
            if (!CheckSettingsLogic.SolveLinuxToolsDir())
            {
                MessageBox.Show(this, _linuxToolsShNotFound.Text, _linuxToolsShNotFoundCaption.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                PageHost.GotoPage(GitSettingsPage.GetPageReference());
                return;
            }

            MessageBox.Show(this, string.Format(_shCanBeRun.Text, AppSettings.LinuxToolsDir), _shCanBeRunCaption.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            PageHost.LoadAll(); // apply settings to dialog controls (otherwise the later called SaveAndRescan_Click would overwrite settings again)
            SaveAndRescan_Click(this, EventArgs.Empty);
        }

        private void ShellExtensionsRegistered_Click(object sender, EventArgs e)
        {
            ShellExtensionManager.Register();

            CheckSettings();
        }

        private void DiffToolFix_Click(object sender, EventArgs e)
        {
            Validates.NotNull(_diffMergeToolConfigurationManager);
            string? diffTool = _diffMergeToolConfigurationManager.ConfiguredDiffTool;
            if (string.IsNullOrEmpty(diffTool))
            {
                GotoPageGlobalSettings();
                return;
            }

            SaveAndRescan_Click(this, EventArgs.Empty);
        }

        private void MergeToolFix_Click(object sender, EventArgs e)
        {
            Validates.NotNull(_diffMergeToolConfigurationManager);
            string? mergeTool = _diffMergeToolConfigurationManager.ConfiguredMergeTool;
            if (string.IsNullOrEmpty(mergeTool))
            {
                GotoPageGlobalSettings();
                return;
            }

            SaveAndRescan_Click(this, EventArgs.Empty);
        }

        private void GotoPageGlobalSettings()
        {
            PageHost.GotoPage(GitConfigSettingsPage.GetPageReference());
        }

        private void UserNameSet_Click(object sender, EventArgs e)
        {
            GotoPageGlobalSettings();

            // nice-to-have: jump directly to correct text box
        }

        private void GitFound_Click(object sender, EventArgs e)
        {
            if (!CheckSettingsLogic.SolveGitCommand())
            {
                MessageBox.Show(this, _solveGitCommandFailed.Text, _solveGitCommandFailedCaption.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);

                PageHost.GotoPage(GitSettingsPage.GetPageReference());
                return;
            }

            MessageBox.Show(this, string.Format(_gitCanBeRun.Text, AppSettings.GitCommandValue), _gitCanBeRunCaption.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

            PageHost.GotoPage(GitSettingsPage.GetPageReference());
            SaveAndRescan_Click(this, EventArgs.Empty);
        }

        private void SaveAndRescan_Click(object sender, EventArgs e)
        {
            using (WaitCursorScope.Enter())
            {
                PageHost.SaveAll();
                PageHost.LoadAll();
                CheckSettings();
            }
        }

        private void CheckAtStartup_CheckedChanged(object sender, EventArgs e)
        {
            AppSettings.CheckSettings = CheckAtStartup.Checked;
        }

        public bool CheckSettings()
        {
            _diffMergeToolConfigurationManager = new DiffMergeToolConfigurationManager(() => CheckSettingsLogic.CommonLogic.GitConfigSettingsSet.EffectiveSettings);

            bool isValid = PerformChecks();
            CheckAtStartup.Checked = IsCheckAtStartupChecked(isValid);
            return isValid;

            bool PerformChecks()
            {
                bool result = true;
                foreach (Func<bool> func in CheckFuncs())
                {
                    try
                    {
                        result &= func();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(this, e.Message, TranslatedStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                return result;

                IEnumerable<Func<bool>> CheckFuncs()
                {
                    yield return CheckGitCmdValid;
                    yield return CheckGlobalUserSettingsValid;
                    yield return CheckEditorTool;
                    yield return CheckMergeTool;
                    yield return CheckDiffToolConfiguration;
                    yield return CheckTranslationConfigSettings;

                    if (EnvUtils.RunningOnWindows())
                    {
                        yield return CheckGitExtensionsInstall;
                        yield return CheckGitExtensionRegistrySettings;
                        yield return CheckGitExe;
                        yield return CheckSSHSettings;
                        yield return CheckGitCredentialWinStore;
                    }
                }
            }
        }

        private static bool IsCheckAtStartupChecked(bool isValid)
        {
            bool retValue = AppSettings.CheckSettings;

            if (isValid && retValue)
            {
                AppSettings.CheckSettings = false;
                retValue = false;
            }

            return retValue;
        }

        /// <summary>
        /// The Git Credential Manager for Windows (GCM) provides secure Git credential storage for Windows.
        /// It's the successor to the Windows Credential Store for Git (git-credential-winstore), which is no longer maintained.
        /// Check whether the user has an outdated setting pointing to git-credential-winstore and, if so,
        /// notify the user and point to our GitHub thread with more information.
        /// </summary>
        /// <seealso href="https://github.com/gitextensions/gitextensions/issues/3511#issuecomment-313633897"/>
        private bool CheckGitCredentialWinStore()
        {
            string setting = GetGlobalSetting(SettingKeyString.CredentialHelper) ?? string.Empty;
            if (setting.IndexOf("git-credential-winstore.exe", StringComparison.OrdinalIgnoreCase) < 0)
            {
                GcmDetected.Visible = false;
                GcmDetectedFix.Visible = false;
                return true;
            }

            GcmDetected.Visible = true;
            GcmDetectedFix.Visible = true;

            RenderSettingUnset(GcmDetected, GcmDetectedFix, _gcmDetectedCaption.Text);
            return false;
        }

        private bool CheckTranslationConfigSettings()
        {
            return RenderSettingSetUnset(() => string.IsNullOrEmpty(AppSettings.Translation),
                                    translationConfig, translationConfig_Fix,
                                    _noLanguageConfigured.Text, string.Format(_languageConfigured.Text, AppSettings.Translation));
        }

        private bool CheckSSHSettings()
        {
            SshConfig.Visible = true;
            if (GitSshHelpers.IsPlink)
            {
                return RenderSettingSetUnset(() => !File.Exists(AppSettings.Plink) || !File.Exists(AppSettings.Puttygen) || !File.Exists(AppSettings.Pageant),
                                        SshConfig, SshConfig_Fix,
                                        _plinkputtyGenpageantNotFound.Text,
                                        _puttyConfigured.Text);
            }

            string ssh = AppSettings.SshPath;
            if (!string.IsNullOrEmpty(ssh) && !File.Exists(ssh))
            {
                RenderSettingUnset(SshConfig, SshConfig_Fix, string.Format(_sshClientNotFound.Text, ssh));
                return false;
            }

            RenderSettingSet(SshConfig, SshConfig_Fix, string.IsNullOrEmpty(ssh) ? _opensshUsed.Text : string.Format(_otherSshClient.Text, ssh));
            return true;
        }

        private bool CheckGitExe()
        {
            return RenderSettingSetUnset(() => !File.Exists(AppSettings.LinuxToolsDir + "sh.exe") && !File.Exists(AppSettings.LinuxToolsDir + "sh") &&
                                         !CheckSettingsLogic.CheckIfFileIsInPath("sh.exe") && !CheckSettingsLogic.CheckIfFileIsInPath("sh"),
                                   GitBinFound, GitBinFound_Fix,
                                   _linuxToolsSshNotFound.Text, _linuxToolsSshFound.Text);
        }

        private bool CheckGitCmdValid()
        {
            GitFound.Visible = true;
            if (!CheckSettingsLogic.CanFindGitCmd())
            {
                RenderSettingUnset(GitFound, GitFound_Fix, _gitNotFound.Text);
                return false;
            }

            IGitVersion nativeGitVersion = GitVersion.Current;
            IGitVersion usedGitVersion = ServiceProvider is IGitUICommands uiCommands && uiCommands.Module.IsValidGitWorkingDir() ? GitVersion.CurrentVersion(uiCommands.Module.GitExecutable) : nativeGitVersion;
            string displayedVersion = nativeGitVersion == usedGitVersion ? $"{nativeGitVersion}" : $"{nativeGitVersion} / WSL {usedGitVersion}";

            if (usedGitVersion < GitVersion.LastSupportedVersion)
            {
                RenderSettingUnset(GitFound, GitFound_Fix, string.Format(_wrongGitVersion.Text, displayedVersion, GitVersion.LastRecommendedVersion));
                return false;
            }

            if (usedGitVersion < GitVersion.LastRecommendedVersion)
            {
                RenderSettingNotRecommended(GitFound, GitFound_Fix, string.Format(_notRecommendedGitVersion.Text, displayedVersion, GitVersion.LastRecommendedVersion));
                return false;
            }

            RenderSettingSet(GitFound, GitFound_Fix, string.Format(_gitVersionFound.Text, displayedVersion));
            return true;
        }

        private bool CheckDiffToolConfiguration()
        {
            Validates.NotNull(_diffMergeToolConfigurationManager);

            DiffTool.Visible = true;
            string? diffTool = _diffMergeToolConfigurationManager.ConfiguredDiffTool;
            if (string.IsNullOrEmpty(diffTool))
            {
                RenderSettingUnset(DiffTool, DiffTool_Fix, _adviceDiffToolConfiguration.Text);
                return false;
            }

            string cmd = _diffMergeToolConfigurationManager.GetToolCommand(diffTool, DiffMergeToolType.Diff);
            if (string.IsNullOrWhiteSpace(cmd))
            {
                RenderSettingUnset(DiffTool, DiffTool_Fix, _adviceDiffToolConfiguration.Text);
                return false;
            }

            RenderSettingSet(DiffTool, DiffTool_Fix, string.Format(_diffToolXConfigured.Text, diffTool));
            return true;
        }

        private bool CheckMergeTool()
        {
            Validates.NotNull(_diffMergeToolConfigurationManager);

            MergeTool.Visible = true;
            string? mergeTool = _diffMergeToolConfigurationManager.ConfiguredMergeTool;
            if (string.IsNullOrEmpty(mergeTool))
            {
                RenderSettingUnset(MergeTool, MergeTool_Fix, _configureMergeTool.Text);
                return false;
            }

            string cmd = _diffMergeToolConfigurationManager.GetToolCommand(mergeTool, DiffMergeToolType.Merge);
            if (string.IsNullOrWhiteSpace(cmd))
            {
                RenderSettingUnset(MergeTool, MergeTool_Fix, string.Format(_mergeToolXConfiguredNeedsCmd.Text, mergeTool));
                return false;
            }

            RenderSettingSet(MergeTool, MergeTool_Fix, string.Format(_mergeToolXConfigured.Text, mergeTool));
            return true;
        }

        private bool CheckGlobalUserSettingsValid()
        {
            return RenderSettingSetUnset(() => string.IsNullOrEmpty(GetGlobalSetting(SettingKeyString.UserName)) ||
                                         string.IsNullOrEmpty(GetGlobalSetting(SettingKeyString.UserEmail)),
                                   UserNameSet, UserNameSet_Fix,
                                   _noEmailSet.Text, _emailSet.Text);
        }

        private bool CheckEditorTool()
        {
            string? editor = CommonLogic.GetGlobalEditor();
            return !string.IsNullOrEmpty(editor);
        }

        private bool CheckGitExtensionRegistrySettings()
        {
            if (!EnvUtils.RunningOnWindows())
            {
                return true;
            }

            ShellExtensionsRegistered.Visible = true;

            if (!ShellExtensionManager.IsRegistered())
            {
                // Check if shell extensions are installed
                if (!ShellExtensionManager.FilesExist())
                {
                    RenderSettingSet(ShellExtensionsRegistered, ShellExtensionsRegistered_Fix, _shellExtNoInstalled.Text);
                    return true;
                }

                RenderSettingUnset(ShellExtensionsRegistered, ShellExtensionsRegistered_Fix, string.Format(_shellExtNeedsToBeRegistered.Text, ShellExtensionManager.GitExtensionsShellEx32Name));
                return false;
            }

            RenderSettingSet(ShellExtensionsRegistered, ShellExtensionsRegistered_Fix, _shellExtRegistered.Text);
            return true;
        }

        private bool CheckGitExtensionsInstall()
        {
            if (!EnvUtils.RunningOnWindows())
            {
                return true;
            }

            GitExtensionsInstall.Visible = true;

            string? installDir = AppSettings.GetInstallDir();

            if (string.IsNullOrEmpty(installDir))
            {
                RenderSettingUnset(GitExtensionsInstall, GitExtensionsInstall_Fix, _registryKeyGitExtensionsMissing.Text);
                return false;
            }

            if (installDir.EndsWith(".exe") || !Directory.Exists(installDir))
            {
                RenderSettingUnset(GitExtensionsInstall, GitExtensionsInstall_Fix, _registryKeyGitExtensionsFaulty.Text);
                return false;
            }

            if (!System.Diagnostics.Debugger.IsAttached && installDir != AppSettings.GetGitExtensionsDirectory())
            {
                RenderSettingUnset(GitExtensionsInstall, GitExtensionsInstall_Fix, _registryKeyGitExtensionsFaulty.Text);
                return false;
            }

            RenderSettingSet(GitExtensionsInstall, GitExtensionsInstall_Fix, _registryKeyGitExtensionsCorrect.Text);
            return true;
        }

        /// <summary>
        /// Renders settings as configured or not depending on the supplied condition.
        /// </summary>
        private static bool RenderSettingSetUnset(Func<bool> condition, Button settingButton, Button settingFixButton,
            string textSettingUnset, string textSettingGood)
        {
            settingButton.Visible = true;
            if (condition())
            {
                RenderSettingUnset(settingButton, settingFixButton, textSettingUnset);
                return false;
            }

            RenderSettingSet(settingButton, settingFixButton, textSettingGood);
            return true;
        }

        /// <summary>
        /// Renders settings as correctly configured.
        /// </summary>
        private static void RenderSettingSet(Button settingButton, Button settingFixButton, string text)
        {
            settingButton.BackColor = OtherColors.BrightGreen;
            settingButton.ForeColor = ColorHelper.GetForeColorForBackColor(settingButton.BackColor);
            settingButton.Text = text;
            settingFixButton.Visible = false;
        }

        /// <summary>
        /// Renders settings as misconfigured.
        /// </summary>
        private static void RenderSettingUnset(Button settingButton, Button settingFixButton, string text)
        {
            settingButton.BackColor = OtherColors.BrightRed;
            settingButton.ForeColor = ColorHelper.GetForeColorForBackColor(settingButton.BackColor);
            settingButton.Text = text;
            settingFixButton.Visible = true;
        }

        private static void RenderSettingNotRecommended(Button settingButton, Button settingFixButton, string text)
        {
            settingButton.BackColor = OtherColors.BrightYellow;
            settingButton.ForeColor = ColorHelper.GetForeColorForBackColor(settingButton.BackColor);
            settingButton.Text = text;
            settingFixButton.Visible = true;
        }

        private void GcmDetectedFix_Click(object sender, EventArgs e)
        {
            OsShellUtil.OpenUrlInDefaultBrowser(@"https://github.com/gitextensions/gitextensions/wiki/Fix-GitCredentialWinStore-missing");
        }
    }
}
