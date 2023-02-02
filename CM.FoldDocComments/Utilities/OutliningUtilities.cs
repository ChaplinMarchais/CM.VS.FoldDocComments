using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text;

using static CM.FoldDocComments.Literals;
using System.Globalization;

namespace CM.FoldDocComments
{
	public static class OutliningUtilities
	{

		public async static Task<IOutliningManager> GetOutliningManagerAsync(DocumentView activeDocView)
		{
			var outliningSvc = await VS.GetMefServiceAsync<IOutliningManagerService>();
			IOutliningManager outliningManager = null;

			uint retryCount = 0;
			while (outliningManager is null && retryCount < 20)
			{
				await Task.Delay(100);
				//TODO: Might require a Task.Delay()
				outliningManager = outliningSvc.GetOutliningManager(activeDocView.TextView);

				retryCount++;
			}

			return outliningManager;
		}
		
		public static bool CollapseRegions(IOutliningManager manager, ConcurrentStack<ICollapsible> regionsToCollapse)
		{
			bool result = false;

			if(regionsToCollapse.Count == 0 || regionsToCollapse is null)
			{
				return true;
			}

			while (regionsToCollapse.Count > 0)
			{
				if (regionsToCollapse.TryPop(out var region))
				{
					if (!region.IsCollapsed)
					{
						result = manager.TryCollapse(region) is not null;
					}
				}
			}

			return result;
		}
	}
}
