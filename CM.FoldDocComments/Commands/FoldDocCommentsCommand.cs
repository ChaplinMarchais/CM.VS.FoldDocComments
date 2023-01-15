using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Outlining;

namespace CM.FoldDocComments
{
	[Command(PackageIds.FoldDocCommentsCommand)]
	internal sealed class FoldDocCommentsCommand : BaseCommand<FoldDocCommentsCommand>
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

			await VS.MessageBox.ShowWarningAsync("CM.FoldDocComments", "FOLD THE DOC COMMENTS NAOW!");
		}

		private async Task<bool> DoWorkOnRegionsAsync(IOutliningManager manager, ITextSnapshot snapshot, IList<ICollapsible> allRegions)
		{
			Stack<ICollapsible> regionsToCollapse = new();

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
							regionsToCollapse.Push(region);
			}

			var result = CollapseRegions(manager, regionsToCollapse);

			return !result.Contains(false);
		}

		private IEnumerable<bool> CollapseRegions(IOutliningManager manager, Stack<ICollapsible> regionsToCollapse)
		{
			while(regionsToCollapse.Count > 0)
			{
				bool result = new();

				var region = regionsToCollapse.Pop();
				if (!region.IsCollapsed)
				{
					result = manager.TryCollapse(region) is ICollapsed;
				}

				yield return result;
			}
		}
	}
}
