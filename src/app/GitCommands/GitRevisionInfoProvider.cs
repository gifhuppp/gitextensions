using GitCommands.Git;
using GitExtensions.Extensibility.Git;

namespace GitCommands
{
    public interface IGitRevisionInfoProvider
    {
        /// <summary>
        /// Loads children item for the given <paramref name="item"/>.
        /// </summary>
        /// <returns>The item's children.</returns>
        IEnumerable<INamedGitItem> LoadChildren(IGitItem item);
    }

    public sealed class GitRevisionInfoProvider : IGitRevisionInfoProvider
    {
        private readonly Func<IGitModule> _getModule;

        public GitRevisionInfoProvider(Func<IGitModule> getModule)
        {
            _getModule = getModule;
        }

        /// <summary>
        /// Loads children item for the given <paramref name="item"/>.
        /// </summary>
        /// <returns>The item's children.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><see cref="IGitItem.Guid"/> is not supplied.</exception>
        public IEnumerable<INamedGitItem> LoadChildren(IGitItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (item.ObjectId is null)
            {
                throw new ArgumentException("Item must have a valid identifier", nameof(item.Guid));
            }

            IGitModule module = _getModule();

            if (module is null)
            {
                throw new ArgumentException($"Require a valid instance of {nameof(IGitModule)}");
            }

            return YieldSubItems();

            IEnumerable<INamedGitItem> YieldSubItems()
            {
                string basePath = (item as GitItem)?.FileName ?? string.Empty;

                foreach (INamedGitItem subItem in module.GetTree(item.ObjectId, full: false))
                {
                    if (subItem is GitItem gitItem)
                    {
                        gitItem.FileName = Path.Combine(basePath, gitItem.FileName);
                    }

                    yield return subItem;
                }
            }
        }
    }
}
