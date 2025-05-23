﻿using System.Text;
using System.Text.RegularExpressions;
using GitCommands;
using GitCommands.Settings;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Settings;
using GitExtUtils;
using ResourceManager;

namespace GitExtensions.Plugins.ProxySwitcher
{
    public partial class ProxySwitcherForm : GitExtensionsFormBase
    {
        private readonly ProxySwitcherPlugin _plugin;
        private readonly SettingsSource _settings;
        private readonly IGitModule _gitCommands;

        #region Translation
        private readonly TranslationString _pluginDescription = new("Proxy Switcher");
        private readonly TranslationString _pleaseSetProxy = new("There is no proxy configured. Please set the proxy host in the plugin settings.");
        #endregion

        [GeneratedRegex(@":(.*)@", RegexOptions.ExplicitCapture)]
        private static partial Regex PasswordRegex();

        /// <summary>
        /// Default constructor added to register all strings to be translated
        /// Use the other constructor:
        /// ProxySwitcherForm(IGitPluginSettingsContainer settings, GitUIBaseEventArgs gitUiCommands)
        /// </summary>
        public ProxySwitcherForm()
        {
            InitializeComponent();

            _plugin = null!;
            _settings = null!;
            _gitCommands = null!;
        }

        public ProxySwitcherForm(ProxySwitcherPlugin plugin, SettingsSource settings, GitUIEventArgs gitUiCommands)
        {
            InitializeComponent();
            InitializeComplete();

            Text = _pluginDescription.Text;
            _plugin = plugin;
            _settings = settings;
            _gitCommands = gitUiCommands.GitModule;
        }

        private void ProxySwitcherForm_Load(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_plugin.HttpProxy.ValueOrDefault(_settings)))
            {
                MessageBox.Show(this, _pleaseSetProxy.Text, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
            else
            {
                RefreshProxy();
            }
        }

        private void RefreshProxy()
        {
            LocalHttpProxy_TextBox.Text = HidePassword(_gitCommands.GetEffectiveSetting("http.proxy"));
            GlobalHttpProxy_TextBox.Text = HidePassword(new GitConfigSettings(_gitCommands.GitExecutable, GitSettingLevel.Global).GetValue("http.proxy") ?? "");
            ApplyGlobally_CheckBox.Checked = string.Equals(LocalHttpProxy_TextBox.Text, GlobalHttpProxy_TextBox.Text);
        }

        private static string HidePassword(string httpProxy)
        {
            return PasswordRegex().Replace(httpProxy, ":****@");
        }

        private string BuildHttpProxy()
        {
            StringBuilder sb = new();
            sb.Append("\"");
            string username = _plugin.Username.ValueOrDefault(_settings);
            if (!string.IsNullOrEmpty(username))
            {
                string password = _plugin.Password.ValueOrDefault(_settings);
                sb.Append(username);
                if (!string.IsNullOrEmpty(password))
                {
                    sb.Append(":");
                    sb.Append(password);
                }

                sb.Append("@");
            }

            sb.Append(_plugin.HttpProxy.ValueOrDefault(_settings));
            string port = _plugin.HttpProxyPort.ValueOrDefault(_settings);
            if (!string.IsNullOrEmpty(port))
            {
                sb.Append(":");
                sb.Append(port);
            }

            sb.Append("\"");
            return sb.ToString();
        }

        private void SetProxy_Button_Click(object sender, EventArgs e)
        {
            string httpProxy = BuildHttpProxy();

            GitArgumentBuilder args = new("config")
            {
                { ApplyGlobally_CheckBox.Checked, "--global" },
                "http.proxy",
                httpProxy
            };
            _gitCommands.GitExecutable.GetOutput(args);

            RefreshProxy();
        }

        private void UnsetProxy_Button_Click(object sender, EventArgs e)
        {
            string arguments = ApplyGlobally_CheckBox.Checked
                ? "config --global --unset http.proxy"
                : "config --unset http.proxy";

            _gitCommands.GitExecutable.GetOutput(arguments);

            RefreshProxy();
        }
    }
}
