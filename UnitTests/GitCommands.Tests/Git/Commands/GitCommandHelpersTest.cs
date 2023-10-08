﻿using GitCommands;
using GitCommands.Git;
using GitCommands.Utils;
using GitExtUtils;
using GitUIPluginInterfaces;
using ResourceManager;

namespace GitCommandsTests.Git_Commands
{
    [TestFixture]
    public class GitCommandsHelperTest
    {
        [Test]
        public void CanGetRelativeDateString()
        {
            AppSettings.CurrentTranslation = "English";

            var now = DateTime.Now;

            Assert.AreEqual("0 seconds ago", LocalizationHelpers.GetRelativeDateString(now, now));
            Assert.AreEqual("1 second ago", LocalizationHelpers.GetRelativeDateString(now, now.AddSeconds(-1)));
            Assert.AreEqual("1 minute ago", LocalizationHelpers.GetRelativeDateString(now, now.AddMinutes(-1)));
            Assert.AreEqual("1 hour ago", LocalizationHelpers.GetRelativeDateString(now, now.AddMinutes(-45)));
            Assert.AreEqual("1 hour ago", LocalizationHelpers.GetRelativeDateString(now, now.AddHours(-1)));
            Assert.AreEqual("1 day ago", LocalizationHelpers.GetRelativeDateString(now, now.AddDays(-1)));
            Assert.AreEqual("1 week ago", LocalizationHelpers.GetRelativeDateString(now, now.AddDays(-7)));
            Assert.AreEqual("1 month ago", LocalizationHelpers.GetRelativeDateString(now, now.AddDays(-30)));
            Assert.AreEqual("12 months ago", LocalizationHelpers.GetRelativeDateString(now, now.AddDays(-364)));
            Assert.AreEqual("1 year ago", LocalizationHelpers.GetRelativeDateString(now, now.AddDays(-365)));

            Assert.AreEqual("2 seconds ago", LocalizationHelpers.GetRelativeDateString(now, now.AddSeconds(-2)));
            Assert.AreEqual("2 minutes ago", LocalizationHelpers.GetRelativeDateString(now, now.AddMinutes(-2)));
            Assert.AreEqual("2 hours ago", LocalizationHelpers.GetRelativeDateString(now, now.AddHours(-2)));
            Assert.AreEqual("2 days ago", LocalizationHelpers.GetRelativeDateString(now, now.AddDays(-2)));
            Assert.AreEqual("2 weeks ago", LocalizationHelpers.GetRelativeDateString(now, now.AddDays(-14)));
            Assert.AreEqual("2 months ago", LocalizationHelpers.GetRelativeDateString(now, now.AddDays(-60)));
            Assert.AreEqual("2 years ago", LocalizationHelpers.GetRelativeDateString(now, now.AddDays(-730)));
        }

        [Test]
        public void CanGetRelativeNegativeDateString()
        {
            AppSettings.CurrentTranslation = "English";

            var now = DateTime.Now;

            Assert.AreEqual("-1 second ago", LocalizationHelpers.GetRelativeDateString(now, now.AddSeconds(1)));
            Assert.AreEqual("-1 minute ago", LocalizationHelpers.GetRelativeDateString(now, now.AddMinutes(1)));
            Assert.AreEqual("-1 hour ago", LocalizationHelpers.GetRelativeDateString(now, now.AddMinutes(45)));
            Assert.AreEqual("-1 hour ago", LocalizationHelpers.GetRelativeDateString(now, now.AddHours(1)));
            Assert.AreEqual("-1 day ago", LocalizationHelpers.GetRelativeDateString(now, now.AddDays(1)));
            Assert.AreEqual("-1 week ago", LocalizationHelpers.GetRelativeDateString(now, now.AddDays(7)));
            Assert.AreEqual("-1 month ago", LocalizationHelpers.GetRelativeDateString(now, now.AddDays(30)));
            Assert.AreEqual("-12 months ago", LocalizationHelpers.GetRelativeDateString(now, now.AddDays(364)));
            Assert.AreEqual("-1 year ago", LocalizationHelpers.GetRelativeDateString(now, now.AddDays(365)));

            Assert.AreEqual("-2 seconds ago", LocalizationHelpers.GetRelativeDateString(now, now.AddSeconds(2)));
            Assert.AreEqual("-2 minutes ago", LocalizationHelpers.GetRelativeDateString(now, now.AddMinutes(2)));
            Assert.AreEqual("-2 hours ago", LocalizationHelpers.GetRelativeDateString(now, now.AddHours(2)));
            Assert.AreEqual("-2 days ago", LocalizationHelpers.GetRelativeDateString(now, now.AddDays(2)));
            Assert.AreEqual("-2 weeks ago", LocalizationHelpers.GetRelativeDateString(now, now.AddDays(14)));
            Assert.AreEqual("-2 months ago", LocalizationHelpers.GetRelativeDateString(now, now.AddDays(60)));
            Assert.AreEqual("-2 years ago", LocalizationHelpers.GetRelativeDateString(now, now.AddDays(730)));
        }

        [Test]
        public void TestFetchArguments()
        {
            // TODO produce a valid working directory
            GitModule module = new(Path.GetTempPath());
            module.LocalConfigFile.SetString("fetch.parallel", null);
            module.LocalConfigFile.SetString("submodule.fetchJobs", null);
            {
                // Specifying a remote and a local branch creates a local branch
                var fetchCmd = module.FetchCmd("origin", "some-branch", "local").Arguments;
                Assert.AreEqual("-c fetch.parallel=0 -c submodule.fetchJobs=0 fetch --progress \"origin\" +some-branch:refs/heads/local --no-tags", fetchCmd);
            }

            {
                var fetchCmd = module.FetchCmd("origin", "some-branch", "local", true).Arguments;
                Assert.AreEqual("-c fetch.parallel=0 -c submodule.fetchJobs=0 fetch --progress \"origin\" +some-branch:refs/heads/local --tags", fetchCmd);
            }

            {
                // Using a URL as remote and passing a local branch creates the branch
                var fetchCmd = module.FetchCmd("https://host.com/repo", "some-branch", "local").Arguments;
                Assert.AreEqual("-c fetch.parallel=0 -c submodule.fetchJobs=0 fetch --progress \"https://host.com/repo\" +some-branch:refs/heads/local --no-tags", fetchCmd);
            }

            {
                // Using a URL as remote and not passing a local branch
                var fetchCmd = module.FetchCmd("https://host.com/repo", "some-branch", null).Arguments;
                Assert.AreEqual("-c fetch.parallel=0 -c submodule.fetchJobs=0 fetch --progress \"https://host.com/repo\" +some-branch --no-tags", fetchCmd);
            }

            {
                // No remote branch -> No local branch
                var fetchCmd = module.FetchCmd("origin", "", "local").Arguments;
                Assert.AreEqual("-c fetch.parallel=0 -c submodule.fetchJobs=0 fetch --progress \"origin\" --no-tags", fetchCmd);
            }

            {
                // Pull doesn't accept a local branch ever
                var fetchCmd = module.PullCmd("origin", "some-branch", false).Arguments;
                Assert.AreEqual("-c fetch.parallel=0 -c submodule.fetchJobs=0 pull --progress \"origin\" +some-branch --no-tags", fetchCmd);
            }

            {
                // Not even for URL remote
                var fetchCmd = module.PullCmd("https://host.com/repo", "some-branch", false).Arguments;
                Assert.AreEqual("-c fetch.parallel=0 -c submodule.fetchJobs=0 pull --progress \"https://host.com/repo\" +some-branch --no-tags", fetchCmd);
            }

            {
                // Pull with rebase
                var fetchCmd = module.PullCmd("origin", "some-branch", true).Arguments;
                Assert.AreEqual("-c fetch.parallel=0 -c submodule.fetchJobs=0 pull --rebase --progress \"origin\" +some-branch --no-tags", fetchCmd);
            }

            {
                // Config test fetch.parallel
                module.LocalConfigFile.SetString("fetch.parallel", "1");
                var fetchCmd = module.FetchCmd("fetch.parallel", "some-branch", "local").Arguments;
                Assert.AreEqual("-c submodule.fetchJobs=0 fetch --progress \"fetch.parallel\" +some-branch:refs/heads/local --no-tags", fetchCmd);
                module.LocalConfigFile.SetString("fetch.parallel", null);
            }

            {
                // Config test submodule.fetchJobs
                module.LocalConfigFile.SetString("submodule.fetchJobs", "0");
                var fetchCmd = module.FetchCmd("origin", "some-branch", "local").Arguments;
                Assert.AreEqual("-c fetch.parallel=0 fetch --progress \"origin\" +some-branch:refs/heads/local --no-tags", fetchCmd);
                module.LocalConfigFile.SetString("submodule.fetchJobs", null);
            }

            {
                // Config test fetch.parallel and submodule.fetchJobs
                module.LocalConfigFile.SetString("fetch.parallel", "8");
                module.LocalConfigFile.SetString("submodule.fetchJobs", "99");
                var fetchCmd = module.FetchCmd("origin", "some-branch", "local").Arguments;
                Assert.AreEqual("fetch --progress \"origin\" +some-branch:refs/heads/local --no-tags", fetchCmd);
                module.LocalConfigFile.SetString("fetch.parallel", null);
                module.LocalConfigFile.SetString("submodule.fetchJobs", null);
            }
        }

        [Test]
        public void TestMergedBranchesCmd([Values(true, false)] bool includeRemote, [Values(true, false)] bool fullRefname,
            [Values(null, "", " ", "HEAD", "1234567890")] string commit)
        {
            string formatArg = fullRefname ? @" --format=""%(refname)""" : string.Empty;
            string remoteArg = includeRemote ? " -a" : string.Empty;
            string commitArg = string.IsNullOrWhiteSpace(commit) ? string.Empty : $" {commit}";
            string expected = $"branch{formatArg}{remoteArg} --merged{commitArg}";

            Assert.AreEqual(expected, Commands.MergedBranches(includeRemote, fullRefname, commit).Arguments);
        }

        [Test]
        public void TestUnsetStagedStatus()
        {
            GitItemStatus item = new("name");
            Assert.AreEqual(item.Staged, StagedStatus.Unset);
        }

        [Test]
        public void SubmoduleSyncCmd()
        {
            string config = "";
            Assert.AreEqual($"{config}submodule sync \"foo\"", Commands.SubmoduleSync("foo").Arguments);
            Assert.AreEqual($"{config}submodule sync", Commands.SubmoduleSync("").Arguments);
            Assert.AreEqual($"{config}submodule sync", Commands.SubmoduleSync(null).Arguments);
        }

        private static IEnumerable<TestCaseData> AddSubmoduleTestCases()
        {
            yield return new TestCaseData("", null);
            yield return new TestCaseData("-c protocol.file.allow=always ", Commands.GetAllowFileConfig());
        }

        [Test, TestCaseSource(nameof(AddSubmoduleTestCases))]
        public void AddSubmoduleCmd(string config, IEnumerable<GitConfigItem> configs)
        {
            Assert.AreEqual(
                $"{config}submodule add -b \"branch\" \"remotepath\" \"localpath\"",
                Commands.AddSubmodule("remotepath", "localpath", "branch", force: false, configs).Arguments);

            Assert.AreEqual(
                $"{config}submodule add \"remotepath\" \"localpath\"",
                Commands.AddSubmodule("remotepath", "localpath", branch: null, force: false, configs).Arguments);

            Assert.AreEqual(
                $"{config}submodule add -f -b \"branch\" \"remotepath\" \"localpath\"",
                Commands.AddSubmodule("remotepath", "localpath", "branch", force: true, configs).Arguments);

            Assert.AreEqual(
                $"{config}submodule add -f -b \"branch\" \"remote/path\" \"local/path\"",
                Commands.AddSubmodule("remote\\path", "local\\path", "branch", force: true, configs).Arguments);
        }

        [Test]
        public void RevertCmd()
        {
            var commitId = ObjectId.Random();

            Assert.AreEqual(
                $"revert {commitId}",
                Commands.Revert(commitId, autoCommit: true, parentIndex: 0).Arguments);

            Assert.AreEqual(
                $"revert --no-commit {commitId}",
                Commands.Revert(commitId, autoCommit: false, parentIndex: 0).Arguments);

            Assert.AreEqual(
                $"revert -m 1 {commitId}",
                Commands.Revert(commitId, autoCommit: true, parentIndex: 1).Arguments);
        }

        [Test]
        public void CloneCmd()
        {
            Assert.AreEqual(
                "clone -v --progress \"from\" \"to\"",
                Commands.Clone("from", "to").Arguments);
            Assert.AreEqual(
                "clone -v --progress \"from/path\" \"to/path\"",
                Commands.Clone("from/path", "to/path").Arguments);
            Assert.AreEqual(
                "clone -v --bare --progress \"from\" \"to\"",
                Commands.Clone("from", "to", central: true).Arguments);
            Assert.AreEqual(
                "clone -v --recurse-submodules --progress \"from\" \"to\"",
                Commands.Clone("from", "to", initSubmodules: true).Arguments);
            Assert.AreEqual(
                "clone -v --recurse-submodules --progress \"from\" \"to\"",
                Commands.Clone("from", "to", initSubmodules: true).Arguments);
            Assert.AreEqual(
                "clone -v --depth 2 --progress \"from\" \"to\"",
                Commands.Clone("from", "to", depth: 2).Arguments);
            Assert.AreEqual(
                "clone -v --single-branch --progress \"from\" \"to\"",
                Commands.Clone("from", "to", isSingleBranch: true).Arguments);
            Assert.AreEqual(
                "clone -v --no-single-branch --progress \"from\" \"to\"",
                Commands.Clone("from", "to", isSingleBranch: false).Arguments);
            Assert.AreEqual(
                "clone -v --progress --branch branch \"from\" \"to\"",
                Commands.Clone("from", "to", branch: "branch").Arguments);
            Assert.AreEqual(
                "clone -v --progress --no-checkout \"from\" \"to\"",
                Commands.Clone("from", "to", branch: null).Arguments);
        }

        [Test]
        public void CheckoutCmd()
        {
            Assert.AreEqual(
                "checkout \"branch\"",
                Commands.Checkout("branch", LocalChangesAction.DontChange).Arguments);
            Assert.AreEqual(
                "checkout --merge \"branch\"",
                Commands.Checkout("branch", LocalChangesAction.Merge).Arguments);
            Assert.AreEqual(
                "checkout --force \"branch\"",
                Commands.Checkout("branch", LocalChangesAction.Reset).Arguments);
            Assert.AreEqual(
                "checkout \"branch\"",
                Commands.Checkout("branch", LocalChangesAction.Stash).Arguments);
        }

        [Test]
        public void RemoveCmd()
        {
            // TODO file names should be quoted

            Assert.AreEqual(
                "rm --force -r .",
                Commands.Remove().Arguments);
            Assert.AreEqual(
                "rm -r .",
                Commands.Remove(force: false).Arguments);
            Assert.AreEqual(
                "rm --force .",
                Commands.Remove(isRecursive: false).Arguments);
            Assert.AreEqual(
                "rm --force -r a b c",
                Commands.Remove(files: new[] { "a", "b", "c" }).Arguments);
        }

        [Test]
        public void BranchCmd()
        {
            // TODO split this into BranchCmd and CheckoutCmd

            Assert.AreEqual(
                "checkout -b \"branch\" \"revision\"",
                Commands.Branch("branch", "revision", checkout: true).Arguments);
            Assert.AreEqual(
                "branch \"branch\" \"revision\"",
                Commands.Branch("branch", "revision", checkout: false).Arguments);
            Assert.AreEqual(
                "checkout -b \"branch\"",
                Commands.Branch("branch", null, checkout: true).Arguments);
            Assert.AreEqual(
                "checkout -b \"branch\"",
                Commands.Branch("branch", "", checkout: true).Arguments);
            Assert.AreEqual(
                "checkout -b \"branch\"",
                Commands.Branch("branch", "  ", checkout: true).Arguments);
        }

        [Test]
        public void PushTagCmd()
        {
            Assert.AreEqual(
                "push --progress \"path\" tag tag",
                Commands.PushTag("path", "tag", all: false).Arguments);
            Assert.AreEqual(
                "push --progress \"path\" tag tag",
                Commands.PushTag("path", " tag ", all: false).Arguments);
            Assert.AreEqual(
                "push --progress \"path/path\" tag tag",
                Commands.PushTag("path\\path", " tag ", all: false).Arguments);
            Assert.AreEqual(
                "push --progress \"path\" --tags",
                Commands.PushTag("path", "tag", all: true).Arguments);
            Assert.AreEqual(
                "push -f --progress \"path\" --tags",
                Commands.PushTag("path", "tag", all: true, force: ForcePushOptions.Force).Arguments);
            Assert.AreEqual(
                "push --force-with-lease --progress \"path\" --tags",
                Commands.PushTag("path", "tag", all: true, force: ForcePushOptions.ForceWithLease).Arguments);

            // TODO this should probably throw rather than return an empty string
            Assert.AreEqual(
                "",
                Commands.PushTag("path", "", all: false).Arguments);
        }

        [Test]
        public void StashSaveCmd()
        {
            // TODO test case where message string contains quotes
            // TODO test case where message string contains newlines
            // TODO test case where selectedFiles contains whitespaces (not currently quoted)

            Assert.AreEqual(
                "stash save",
                Commands.StashSave(untracked: false, keepIndex: false, null, Array.Empty<string>()).Arguments);

            Assert.AreEqual(
                "stash save",
                Commands.StashSave(untracked: false, keepIndex: false, null, null).Arguments);

            Assert.AreEqual(
                "stash save -u",
                Commands.StashSave(untracked: true, keepIndex: false, null, null).Arguments);

            Assert.AreEqual(
                "stash save --keep-index",
                Commands.StashSave(untracked: false, keepIndex: true, null, null).Arguments);

            Assert.AreEqual(
                "stash save --keep-index",
                Commands.StashSave(untracked: false, keepIndex: true, null, null).Arguments);

            Assert.AreEqual(
                "stash save \"message\"",
                Commands.StashSave(untracked: false, keepIndex: false, "message", null).Arguments);

            Assert.AreEqual(
                "stash push -- \"a\" \"b\"",
                Commands.StashSave(untracked: false, keepIndex: false, null, new[] { "a", "b" }).Arguments);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("\t")]
        public void StashSaveCmd_should_not_add_empty_message_full_stash(string theMessage)
        {
            Assert.AreEqual(
               "stash save",
               Commands.StashSave(untracked: false, keepIndex: false, theMessage, Array.Empty<string>()).Arguments);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("\t")]
        public void StashPushCmd_should_not_add_empty_message_partial_stash(string theMessage)
        {
            Assert.AreEqual(
               "stash push -- \"a\" \"b\"",
               Commands.StashSave(untracked: false, keepIndex: false, theMessage, new[] { "a", "b" }).Arguments);
        }

        [Test]
        public void StashSaveCmd_should_add_message_if_provided_full_stash()
        {
            Assert.AreEqual(
               "stash save \"test message\"",
               Commands.StashSave(untracked: false, keepIndex: false, "test message", Array.Empty<string>()).Arguments);
        }

        [Test]
        public void StashPushCmd_should_add_message_if_provided_partial_stash()
        {
            Assert.AreEqual(
               "stash push -m \"test message\" -- \"a\" \"b\"",
               Commands.StashSave(untracked: false, keepIndex: false, "test message", new[] { "a", "b" }).Arguments);
        }

        [Test]
        public void StashPushCmd_should_not_add_null_or_empty_filenames()
        {
            Assert.AreEqual(
               "stash push -- \"a\"",
               Commands.StashSave(untracked: false, keepIndex: false, null, new[] { null, "", "a" }).Arguments);
        }

        [Test]
        public void ContinueBisectCmd()
        {
            var id1 = ObjectId.Random();
            var id2 = ObjectId.Random();

            Assert.AreEqual(
                "bisect good",
                Commands.ContinueBisect(GitBisectOption.Good).Arguments);
            Assert.AreEqual(
                "bisect bad",
                Commands.ContinueBisect(GitBisectOption.Bad).Arguments);
            Assert.AreEqual(
                "bisect skip",
                Commands.ContinueBisect(GitBisectOption.Skip).Arguments);
            Assert.AreEqual(
                $"bisect good {id1} {id2}",
                Commands.ContinueBisect(GitBisectOption.Good, id1, id2).Arguments);
        }

        [Test]
        public void RebaseCmd()
        {
            Assert.AreEqual(
                "-c rebase.autoSquash=false rebase \"branch\"",
                Commands.Rebase("branch", interactive: false, preserveMerges: false, autosquash: false, autoStash: false, ignoreDate: false, committerDateIsAuthorDate: false).Arguments);
            Assert.AreEqual(
                "-c rebase.autoSquash=false rebase -i --no-autosquash \"branch\"",
                Commands.Rebase("branch", interactive: true, preserveMerges: false, autosquash: false, autoStash: false, ignoreDate: false, committerDateIsAuthorDate: false).Arguments);
            Assert.AreEqual(
                "-c rebase.autoSquash=false rebase --rebase-merges \"branch\"",
                Commands.Rebase("branch", interactive: false, preserveMerges: true, autosquash: false, autoStash: false, ignoreDate: false, committerDateIsAuthorDate: false, supportRebaseMerges: true).Arguments);
            Assert.AreEqual(
                "-c rebase.autoSquash=false rebase \"branch\"",
                Commands.Rebase("branch", interactive: false, preserveMerges: false, autosquash: true, autoStash: false, ignoreDate: false, committerDateIsAuthorDate: false).Arguments);
            Assert.AreEqual(
                "-c rebase.autoSquash=false rebase --autostash \"branch\"",
                Commands.Rebase("branch", interactive: false, preserveMerges: false, autosquash: false, autoStash: true, ignoreDate: false, committerDateIsAuthorDate: false).Arguments);
            Assert.AreEqual(
                "-c rebase.autoSquash=false rebase -i --autosquash \"branch\"",
                Commands.Rebase("branch", interactive: true, preserveMerges: false, autosquash: true, autoStash: false, ignoreDate: false, committerDateIsAuthorDate: false).Arguments);
            Assert.AreEqual(
                "-c rebase.autoSquash=false rebase --ignore-date \"branch\"",
                Commands.Rebase("branch", interactive: false, preserveMerges: false, autosquash: false, autoStash: false, ignoreDate: true, committerDateIsAuthorDate: false).Arguments);
            Assert.AreEqual(
                "-c rebase.autoSquash=false rebase --committer-date-is-author-date \"branch\"",
                Commands.Rebase("branch", interactive: false, preserveMerges: false, autosquash: false, autoStash: false, ignoreDate: false, committerDateIsAuthorDate: true).Arguments);
            Assert.AreEqual(
                "-c rebase.autoSquash=false rebase --ignore-date --autostash \"branch\"",
                Commands.Rebase("branch", interactive: false, preserveMerges: false, autosquash: false, autoStash: true, ignoreDate: true, committerDateIsAuthorDate: false).Arguments);
            Assert.AreEqual(
                "-c rebase.autoSquash=false rebase --committer-date-is-author-date --autostash \"branch\"",
                Commands.Rebase("branch", interactive: false, preserveMerges: false, autosquash: false, autoStash: true, ignoreDate: false, committerDateIsAuthorDate: true).Arguments);
            Assert.AreEqual(
                "-c rebase.autoSquash=false rebase --ignore-date --autostash \"branch\"",
                Commands.Rebase("branch", interactive: true, preserveMerges: true, autosquash: true, autoStash: true, ignoreDate: true, committerDateIsAuthorDate: false).Arguments);
            Assert.AreEqual(
                "-c rebase.autoSquash=false rebase --committer-date-is-author-date --autostash \"branch\"",
                Commands.Rebase("branch", interactive: true, preserveMerges: true, autosquash: true, autoStash: true, ignoreDate: false, committerDateIsAuthorDate: true).Arguments);
            Assert.AreEqual(
                "-c rebase.autoSquash=false rebase -i --autosquash --rebase-merges --autostash \"branch\"",
                Commands.Rebase("branch", interactive: true, preserveMerges: true, autosquash: true, autoStash: true, ignoreDate: false, committerDateIsAuthorDate: false, supportRebaseMerges: true).Arguments);

            // TODO quote 'onto'?

            Assert.AreEqual(
                "-c rebase.autoSquash=false rebase \"from\" \"branch\" --onto onto",
                Commands.Rebase("branch", interactive: false, preserveMerges: false, autosquash: false, autoStash: false, ignoreDate: false, committerDateIsAuthorDate: false, "from", "onto").Arguments);
            Assert.AreEqual(
                "-c rebase.autoSquash=false rebase --ignore-date \"from\" \"branch\" --onto onto",
                Commands.Rebase("branch", interactive: false, preserveMerges: false, autosquash: false, autoStash: false, ignoreDate: true, committerDateIsAuthorDate: false, "from", "onto").Arguments);

            Assert.Throws<ArgumentException>(
                () => Commands.Rebase("branch", false, false, false, false, false, false, from: null, onto: "onto"));

            Assert.Throws<ArgumentException>(
                () => Commands.Rebase("branch", false, false, false, false, false, false, from: "from", onto: null));
        }

        [TestCase(CleanMode.OnlyNonIgnored, true, false, null, null, "clean --dry-run")]
        [TestCase(CleanMode.OnlyNonIgnored, false, false, null, null, "clean -f")]
        [TestCase(CleanMode.OnlyNonIgnored, false, true, null, null, "clean -d -f")]
        [TestCase(CleanMode.OnlyNonIgnored, false, false, "\"path1\"", null, "clean -f \"path1\"")]
        [TestCase(CleanMode.OnlyNonIgnored, false, false, "\"path1\"", "--exclude=excludes", "clean -f \"path1\" --exclude=excludes")]
        [TestCase(CleanMode.OnlyNonIgnored, false, false, null, "excludes", "clean -f excludes")]
        [TestCase(CleanMode.OnlyNonIgnored, false, false, "\"path1\" \"path2\"", null, "clean -f \"path1\" \"path2\"")]
        [TestCase(CleanMode.OnlyNonIgnored, false, false, "\"path1\" \"path2\"", "--exclude=exclude1 --exclude=exclude2", "clean -f \"path1\" \"path2\" --exclude=exclude1 --exclude=exclude2")]
        [TestCase(CleanMode.OnlyNonIgnored, false, false, null, "--exclude=exclude1 --exclude=exclude2", "clean -f --exclude=exclude1 --exclude=exclude2")]
        [TestCase(CleanMode.OnlyIgnored, false, false, null, null, "clean -X -f")]
        [TestCase(CleanMode.All, false, false, null, null, "clean -x -f")]
        public void CleanCmd(CleanMode mode, bool dryRun, bool directories, string paths, string excludes, string expected)
        {
            Assert.AreEqual(expected, Commands.Clean(mode, dryRun, directories, paths, excludes).Arguments);
        }

        [TestCase(CleanMode.OnlyNonIgnored, true, false, null, "clean --dry-run")]
        [TestCase(CleanMode.OnlyNonIgnored, false, false, null, "clean -f")]
        [TestCase(CleanMode.OnlyNonIgnored, false, true, null, "clean -d -f")]
        [TestCase(CleanMode.OnlyNonIgnored, false, false, "paths", "clean -f paths")]
        [TestCase(CleanMode.OnlyIgnored, false, false, null, "clean -X -f")]
        [TestCase(CleanMode.All, false, false, null, "clean -x -f")]
        public void CleanupSubmoduleCommand(CleanMode mode, bool dryRun, bool directories, string paths, string expected)
        {
            string subExpected = "submodule foreach --recursive git " + expected;
            Assert.AreEqual(subExpected, Commands.CleanSubmodules(mode, dryRun, directories, paths).Arguments);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("\t")]
        public void ResetCmd_should_throw_if_ResetIndex_and_hash_is_null_or_empty(string hash)
        {
            Assert.Throws<ArgumentException>(
                () => Commands.Reset(ResetMode.ResetIndex, commit: hash, file: "file.txt"));
        }

        [TestCase("mybranch", ".", false, ExpectedResult = @"push ""file://."" ""1111111111111111111111111111111111111111:mybranch""")]
        [TestCase("branch2", "/my/path", true, ExpectedResult = @"push ""file:///my/path"" ""1111111111111111111111111111111111111111:branch2"" --force")]
        [TestCase("branchx", @"c:/my/path", true, ExpectedResult = @"push ""file://c:/my/path"" ""1111111111111111111111111111111111111111:branchx"" --force")]
        public string PushLocalCmd(string gitRef, string repoDir, bool force)
        {
            return Commands.PushLocal(gitRef, ObjectId.WorkTreeId, repoDir, force).Arguments;
        }

        [TestCase(ResetMode.ResetIndex, "tree-ish", null, @"reset ""tree-ish"" --")]
        [TestCase(ResetMode.ResetIndex, "tree-ish", "file.txt", @"reset ""tree-ish"" -- ""file.txt""")]
        [TestCase(ResetMode.Soft, null, null, @"reset --soft --")]
        [TestCase(ResetMode.Mixed, null, null, @"reset --mixed --")]
        [TestCase(ResetMode.Hard, null, null, @"reset --hard --")]
        [TestCase(ResetMode.Merge, null, null, @"reset --merge --")]
        [TestCase(ResetMode.Keep, null, null, @"reset --keep --")]
        [TestCase(ResetMode.Soft, "tree-ish", null, @"reset --soft ""tree-ish"" --")]
        [TestCase(ResetMode.Mixed, "tree-ish", null, @"reset --mixed ""tree-ish"" --")]
        [TestCase(ResetMode.Hard, "tree-ish", null, @"reset --hard ""tree-ish"" --")]
        [TestCase(ResetMode.Merge, "tree-ish", null, @"reset --merge ""tree-ish"" --")]
        [TestCase(ResetMode.Keep, "tree-ish", null, @"reset --keep ""tree-ish"" --")]
        [TestCase(ResetMode.Soft, null, "file.txt", @"reset --soft -- ""file.txt""")]
        [TestCase(ResetMode.Mixed, null, "file.txt", @"reset --mixed -- ""file.txt""")]
        [TestCase(ResetMode.Hard, null, "file.txt", @"reset --hard -- ""file.txt""")]
        [TestCase(ResetMode.Merge, null, "file.txt", @"reset --merge -- ""file.txt""")]
        [TestCase(ResetMode.Keep, null, "file.txt", @"reset --keep -- ""file.txt""")]
        [TestCase(ResetMode.Soft, "tree-ish", "file.txt", @"reset --soft ""tree-ish"" -- ""file.txt""")]
        [TestCase(ResetMode.Mixed, "tree-ish", "file.txt", @"reset --mixed ""tree-ish"" -- ""file.txt""")]
        [TestCase(ResetMode.Hard, "tree-ish", "file.txt", @"reset --hard ""tree-ish"" -- ""file.txt""")]
        [TestCase(ResetMode.Merge, "tree-ish", "file.txt", @"reset --merge ""tree-ish"" -- ""file.txt""")]
        [TestCase(ResetMode.Keep, "tree-ish", "file.txt", @"reset --keep ""tree-ish"" -- ""file.txt""")]
        public void ResetCmd(ResetMode mode, string commit, string file, string expected)
        {
            Assert.AreEqual(expected, Commands.Reset(mode, commit, file).Arguments);
        }

        [Test]
        public void GetAllChangedFilesCmd()
        {
            Assert.AreEqual(
                "-c diff.ignoreSubmodules=none status --porcelain=2 -z --untracked-files --ignore-submodules",
                Commands.GetAllChangedFiles(excludeIgnoredFiles: true, UntrackedFilesMode.Default, IgnoreSubmodulesMode.Default).Arguments);
            Assert.AreEqual(
                "-c diff.ignoreSubmodules=none status --porcelain=2 -z --untracked-files --ignored --ignore-submodules",
                Commands.GetAllChangedFiles(excludeIgnoredFiles: false, UntrackedFilesMode.Default, IgnoreSubmodulesMode.Default).Arguments);
            Assert.AreEqual(
                "-c diff.ignoreSubmodules=none status --porcelain=2 -z --untracked-files=no --ignore-submodules",
                Commands.GetAllChangedFiles(excludeIgnoredFiles: true, UntrackedFilesMode.No, IgnoreSubmodulesMode.Default).Arguments);
            Assert.AreEqual(
                "-c diff.ignoreSubmodules=none status --porcelain=2 -z --untracked-files=normal --ignore-submodules",
                Commands.GetAllChangedFiles(excludeIgnoredFiles: true, UntrackedFilesMode.Normal, IgnoreSubmodulesMode.Default).Arguments);
            Assert.AreEqual(
                "-c diff.ignoreSubmodules=none status --porcelain=2 -z --untracked-files=all --ignore-submodules",
                Commands.GetAllChangedFiles(excludeIgnoredFiles: true, UntrackedFilesMode.All, IgnoreSubmodulesMode.Default).Arguments);
            Assert.AreEqual(
                "-c diff.ignoreSubmodules=none status --porcelain=2 -z --untracked-files",
                Commands.GetAllChangedFiles(excludeIgnoredFiles: true, UntrackedFilesMode.Default, IgnoreSubmodulesMode.None).Arguments);
            Assert.AreEqual(
                "-c diff.ignoreSubmodules=none status --porcelain=2 -z --untracked-files",
                Commands.GetAllChangedFiles(excludeIgnoredFiles: true, UntrackedFilesMode.Default).Arguments);
            Assert.AreEqual(
                "-c diff.ignoreSubmodules=none status --porcelain=2 -z --untracked-files --ignore-submodules=untracked",
                Commands.GetAllChangedFiles(excludeIgnoredFiles: true, UntrackedFilesMode.Default, IgnoreSubmodulesMode.Untracked).Arguments);
            Assert.AreEqual(
                "-c diff.ignoreSubmodules=none status --porcelain=2 -z --untracked-files --ignore-submodules=dirty",
                Commands.GetAllChangedFiles(excludeIgnoredFiles: true, UntrackedFilesMode.Default, IgnoreSubmodulesMode.Dirty).Arguments);
            Assert.AreEqual(
                "-c diff.ignoreSubmodules=none status --porcelain=2 -z --untracked-files --ignore-submodules=all",
                Commands.GetAllChangedFiles(excludeIgnoredFiles: true, UntrackedFilesMode.Default, IgnoreSubmodulesMode.All).Arguments);
            Assert.AreEqual(
                "--no-optional-locks -c diff.ignoreSubmodules=none status --porcelain=2 -z --untracked-files --ignore-submodules",
                Commands.GetAllChangedFiles(excludeIgnoredFiles: true, UntrackedFilesMode.Default, IgnoreSubmodulesMode.Default, noLocks: true).Arguments);
        }

        // Don't care about permutations because the args aren't correlated
        [TestCase(false, false, false, null, false, null, null, "merge --no-ff branch")]
        [TestCase(true, true, true, null, true, null, null, "merge --squash --no-commit --allow-unrelated-histories branch")]

        // mergeCommitFilePath parameter
        [TestCase(false, true, false, null, false, "", null, "merge --no-ff --squash branch")]
        [TestCase(false, true, false, null, false, "   ", null, "merge --no-ff --squash branch")]
        [TestCase(false, true, false, null, false, "\t", null, "merge --no-ff --squash branch")]
        [TestCase(false, true, false, null, false, "\n", null, "merge --no-ff --squash branch")]
        [TestCase(false, true, false, null, false, "foo", null, "merge --no-ff --squash -F \"foo\" branch")]
        [TestCase(false, true, false, null, false, "D:\\myrepo\\.git\\file", null, "merge --no-ff --squash -F \"D:\\myrepo\\.git\\file\" branch")]

        // log parameter
        [TestCase(true, true, false, null, false, null, -1, "merge --squash branch")]
        [TestCase(true, true, false, null, false, null, 0, "merge --squash branch")]
        [TestCase(true, true, false, null, false, null, 5, "merge --squash --log=5 branch")]
        public void MergeBranchCmd(bool allowFastForward, bool squash, bool noCommit, string strategy, bool allowUnrelatedHistories, string mergeCommitFilePath, int? log, string expected)
        {
            Assert.AreEqual(expected, Commands.MergeBranch("branch", allowFastForward, squash, noCommit, strategy, allowUnrelatedHistories, mergeCommitFilePath, log).Arguments);
        }

        [Test]
        public void ContinueMergeCmd()
        {
            Assert.AreEqual("merge --continue", Commands.ContinueMerge().Arguments);
        }

        [Test]
        public void AbortMergeCmd()
        {
            Assert.AreEqual("merge --abort", Commands.AbortMerge().Arguments);
        }

        [Test]
        public void ApplyDiffPatchCmd()
        {
            Assert.AreEqual(
                "apply \"hello/world.patch\"",
                Commands.ApplyDiffPatch(false, "hello\\world.patch").Arguments);
            Assert.AreEqual(
                "apply --ignore-whitespace \"hello/world.patch\"",
                Commands.ApplyDiffPatch(true, "hello\\world.patch").Arguments);
        }

        [TestCase(false, false, "hello\\world.patch", "am --3way \"hello/world.patch\"")]
        [TestCase(false, true, "hello\\world.patch", "am --3way --ignore-whitespace \"hello/world.patch\"")]
        [TestCase(true, false, "hello\\world.patch", "am --3way --signoff \"hello/world.patch\"")]
        [TestCase(true, true, "hello\\world.patch", "am --3way --signoff --ignore-whitespace \"hello/world.patch\"")]
        [TestCase(true, true, null, "am --3way --signoff --ignore-whitespace")]
        public void ApplyMailboxPatchCmd(bool signOff, bool ignoreWhitespace, string patchFile, string expected)
        {
            Assert.AreEqual(
                expected,
                Commands.ApplyMailboxPatch(signOff, ignoreWhitespace, patchFile).Arguments);
        }

        [TestCase(@"-c color.ui=never -c diff.submodule=short -c diff.noprefix=false -c diff.mnemonicprefix=false -c diff.ignoreSubmodules=none -c core.safecrlf=false diff --find-renames --find-copies extra --cached -- ""new"" ""old""", "new", "old", true, "extra", false)]
        [TestCase(@"-c color.ui=never -c diff.submodule=short -c diff.noprefix=false -c diff.mnemonicprefix=false -c diff.ignoreSubmodules=none -c core.safecrlf=false diff --find-renames --find-copies extra -- ""new""", "new", "old", false, "extra", false)]
        [TestCase(@"--no-optional-locks -c color.ui=never -c diff.submodule=short -c diff.noprefix=false -c diff.mnemonicprefix=false -c diff.ignoreSubmodules=none -c core.safecrlf=false diff --find-renames --find-copies extra --cached -- ""new"" ""old""", "new", "old", true, "extra", true)]
        public void GetCurrentChangesCmd(string expected, string fileName, string oldFileName, bool staged,
            string extraDiffArguments, bool noLocks)
        {
            Assert.AreEqual(expected, Commands.GetCurrentChanges(fileName, oldFileName, staged,
                extraDiffArguments, noLocks).ToString());
        }

        private static IEnumerable<TestCaseData> GetRefsCommandTestData
        {
            get
            {
                foreach (GitRefsSortBy sortBy in EnumHelper.GetValues<GitRefsSortBy>())
                {
                    foreach (GitRefsSortOrder sortOrder in EnumHelper.GetValues<GitRefsSortOrder>())
                    {
                        string sortCondition;
                        string sortConditionRef;
                        string format = @" --format=""%(if)%(authordate)%(then)%(objectname) %(refname)%(else)%(*objectname) %(*refname)%(end)""";
                        string formatNoTag = @" --format=""%(objectname) %(refname)""";
                        if (sortBy == GitRefsSortBy.Default)
                        {
                            sortCondition = sortConditionRef = string.Empty;
                        }
                        else
                        {
                            const string gitRefsSortByVersion = "version:refname";
                            string sortKey = sortBy == GitRefsSortBy.versionRefname ? gitRefsSortByVersion : sortBy.ToString();
                            string derefSortKey = (sortBy == GitRefsSortBy.versionRefname ? GitRefsSortBy.refname : sortBy).ToString();

                            string order = sortOrder == GitRefsSortOrder.Ascending ? string.Empty : "-";
                            sortCondition = $@" --sort=""{order}{sortKey}""";
                            sortConditionRef = $@" --sort=""{order}*{derefSortKey}""";
                        }

                        yield return new TestCaseData(RefsFilter.Tags | RefsFilter.Heads | RefsFilter.Remotes, /* noLocks */ false, sortBy, sortOrder, 0,
                            /* expected */ $@"for-each-ref{sortConditionRef}{sortCondition}{format} refs/heads/ refs/remotes/ refs/tags/");
                        yield return new TestCaseData(RefsFilter.Tags, /* noLocks */ false, sortBy, sortOrder, 0,
                            /* expected */ $@"for-each-ref{sortConditionRef}{sortCondition}{format} refs/tags/");
                        yield return new TestCaseData(RefsFilter.Heads, /* noLocks */ false, sortBy, sortOrder, 0,
                            /* expected */ $@"for-each-ref{sortCondition}{formatNoTag} refs/heads/");
                        yield return new TestCaseData(RefsFilter.Heads, /* noLocks */ false, sortBy, sortOrder, 100,
                            /* expected */ $@"for-each-ref{sortCondition}{formatNoTag} --count=100 refs/heads/");
                        yield return new TestCaseData(RefsFilter.Heads, /* noLocks */ true, sortBy, sortOrder, 0,
                            /* expected */ $@"--no-optional-locks for-each-ref{sortCondition}{formatNoTag} refs/heads/");
                        yield return new TestCaseData(RefsFilter.Remotes, /* noLocks */ false, sortBy, sortOrder, 0,
                            /* expected */ $@"for-each-ref{sortCondition}{formatNoTag} refs/remotes/");

                        yield return new TestCaseData(RefsFilter.NoFilter, /* noLocks */ true, sortBy, sortOrder, 0,
                            /* expected */ $@"--no-optional-locks for-each-ref{sortConditionRef}{sortCondition}{format}");
                        yield return new TestCaseData(RefsFilter.Tags | RefsFilter.Heads | RefsFilter.Remotes | RefsFilter.NoFilter, /* noLocks */ true, sortBy, sortOrder, 0,
                            /* expected */ $@"--no-optional-locks for-each-ref{sortConditionRef}{sortCondition}{format} refs/heads/ refs/remotes/ refs/tags/");
                    }
                }
            }
        }

        [TestCaseSource(nameof(GetRefsCommandTestData))]
        public void GetRefsCmd(RefsFilter getRefs, bool noLocks, GitRefsSortBy sortBy, GitRefsSortOrder sortOrder, int count, string expected)
        {
            Assert.AreEqual(expected, Commands.GetRefs(getRefs, noLocks, sortBy, sortOrder, count).ToString());
        }
    }
}
