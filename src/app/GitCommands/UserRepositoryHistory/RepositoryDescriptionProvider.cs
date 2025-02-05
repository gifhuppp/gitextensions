using System.Text.RegularExpressions;
using GitCommands.Git;

namespace GitCommands.UserRepositoryHistory
{
    public interface IRepositoryDescriptionProvider
    {
        /// <summary>
        /// Returns a short name for repository.
        /// If the repository contains a description it is returned,
        /// otherwise the last part of path is returned.
        /// </summary>
        /// <param name="repositoryDir">Path to repository.</param>
        /// <returns>Short name for repository.</returns>
        string Get(string repositoryDir);
    }

    /// <summary>
    ///  Provides the ability to read .git/description file.
    /// </summary>
    /// <remarks>
    ///  https://git-scm.com/book/en/v2/Git-Internals-Plumbing-and-Porcelain:
    ///  ...The description file is used only by the GitWeb program, so don�t worry about it...
    /// </remarks>
    public sealed class RepositoryDescriptionProvider : IRepositoryDescriptionProvider
    {
        private const string RepositoryDescriptionFileName = "description";
        private const string DefaultDescription = "Unnamed repository; edit this file 'description' to name the repository.";

        private readonly Regex _uninformativeNameRegex = new($"^{AppSettings.UninformativeRepoNameRegex.Value}$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        private readonly IGitDirectoryResolver _gitDirectoryResolver;

        public RepositoryDescriptionProvider(IGitDirectoryResolver gitDirectoryResolver)
        {
            _gitDirectoryResolver = gitDirectoryResolver;
        }

        /// <summary>
        /// Returns a short name for repository.
        /// If the repository contains a description it is returned,
        /// otherwise the last part of path is returned.
        /// </summary>
        /// <param name="repositoryDir">Path to repository.</param>
        /// <returns>Short name for repository.</returns>
        public string Get(string repositoryDir)
        {
            DirectoryInfo dirInfo = new(repositoryDir);
            if (!dirInfo.Exists)
            {
                return dirInfo.Name;
            }

            string? desc = ReadRepositoryDescription(repositoryDir);
            if (!string.IsNullOrWhiteSpace(desc))
            {
                return desc;
            }

            while (dirInfo.Parent is not null && _uninformativeNameRegex.IsMatch(dirInfo.Name))
            {
                dirInfo = dirInfo.Parent;
            }

            return dirInfo.Name;
        }

        /// <summary>
        /// Reads repository description's first line from ".git\description" file.
        /// </summary>
        /// <param name="workingDir">Path to repository.</param>
        /// <returns>If the repository has description, returns that description, else returns <c>null</c>.</returns>
        private string? ReadRepositoryDescription(string workingDir)
        {
            string gitDir = _gitDirectoryResolver.Resolve(workingDir);
            string descriptionFilePath = Path.Combine(gitDir, RepositoryDescriptionFileName);

            if (!File.Exists(descriptionFilePath))
            {
                return null;
            }

            try
            {
                string repositoryDescription = File.ReadLines(descriptionFilePath).FirstOrDefault();
                return string.Equals(repositoryDescription, DefaultDescription, StringComparison.CurrentCulture)
                    ? null
                    : repositoryDescription;
            }
            catch (IOException)
            {
                return null;
            }
        }
    }
}
