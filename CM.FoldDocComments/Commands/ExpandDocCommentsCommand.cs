using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace CM.FoldDocComments
{
    [Command(PackageIds.ExpandDocCommentsCommand)]
    internal sealed class ExpandDocCommentsCommand : BaseCommand<ExpandDocCommentsCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var activeDocView = await VS.Documents.GetActiveDocumentViewAsync();

            if (activeDocView is null)
            {
                await VS.StatusBar.ShowMessageAsync("ExpandDocComments could not find an active document");

                return;
            }

            var outliningSvc = await VS.GetMefServiceAsync<IOutliningManagerService>();

            IOutliningManager outliningManager = null;

            uint retryCount = 0;
            while (outliningManager is null && retryCount < 20)
            {
                await Task.Delay(100);

                outliningManager = outliningSvc.GetOutliningManager(activeDocView.TextView);

                retryCount++;
            }

            var snapshot = activeDocView.TextView.TextSnapshot;
            var allRegions = outliningManager.GetAllRegions(new SnapshotSpan(snapshot, 0, snapshot.Length)).ToList();

            await FindAndExpandRegionsAsync(outliningManager, snapshot, allRegions);
        }

        private async Task<bool> FindAndExpandRegionsAsync(IOutliningManager manager, ITextSnapshot snapshot, IList<ICollapsible> allRegions)
        {
            Queue<ICollapsible> docCommentRegions = new();

            foreach (var region in allRegions)
            {
                var regionMatch = true;

                var regionText = region.Extent.GetText(snapshot);

                var lines = regionText.Split('\n');
                foreach (var line in lines)
                {
                    if (!line.TrimStart(' ', '\t').StartsWith("///"))
                    {
                        regionMatch = false;
                    }

                    if (!regionMatch)
                        break;
                }

                if (regionMatch)
                {
                    docCommentRegions.Enqueue(region);
                }
            }

            var result = ExpandRegions(manager, docCommentRegions);

            return !result.Contains(false);
        }

        private IEnumerable<bool> ExpandRegions(IOutliningManager manager, Queue<ICollapsible> docCommentRegions)
        {
            while (docCommentRegions.Count > 0)
            {
                bool result = new();

                var region = docCommentRegions.Dequeue();
                if (region is ICollapsed collapsedRegion)
                {
                    result = manager.Expand(collapsedRegion) is not ICollapsed;
                }

                yield return result;
            }
        }
    }
}
