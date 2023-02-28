using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Threading;

namespace CM.FoldDocComments
{
	[Command(PackageIds.CollapseDocCommentsCommand)]
	internal sealed class CollapseDocCommentsCommand : BaseCommand<CollapseDocCommentsCommand>
	{
		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			var activeDocView = await VS.Documents.GetActiveDocumentViewAsync();

			if (activeDocView is null)
			{
				await VS.StatusBar.ShowMessageAsync("FoldDocCommand could not find an active document");

				return;
			}

			var outliningSvc = await VS.GetMefServiceAsync<IOutliningManagerService>();

			IOutliningManager outliningManager = null;

			uint retryCount = 0;
			while(outliningManager is null && retryCount < 20)
			{
				await Task.Delay(100);
				//TODO: Might require a Task.Delay()
				outliningManager = outliningSvc.GetOutliningManager(activeDocView.TextView);

				retryCount++;
			}

			var snapshot = activeDocView.TextView.TextSnapshot;
			var allRegions = outliningManager.GetAllRegions(new SnapshotSpan(snapshot, 0, snapshot.Length)).ToList();

			await DoWorkOnRegionsAsync(outliningManager, snapshot, allRegions);
		}

		private async Task<bool> DoWorkOnRegionsAsync(IOutliningManager manager, ITextSnapshot snapshot, IList<ICollapsible> allRegions)
		{
			Queue<ICollapsible> regionsToCollapse = new Queue<ICollapsible>();

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
					regionsToCollapse.Enqueue(region);
                }
			}

            var result = CollapseRegions(manager, regionsToCollapse);

            return !result.Contains(false);
        }

        private IEnumerable<bool> CollapseRegions(IOutliningManager manager, Queue<ICollapsible> regionsToCollapse)
		{
			while(regionsToCollapse.Count > 0)
			{
				bool result = new();

				var region = regionsToCollapse.Dequeue();
				if (!region.IsCollapsed)
				{
					result = manager.TryCollapse(region) is ICollapsed;
				}

				yield return result;
			}
		}
	}
}
