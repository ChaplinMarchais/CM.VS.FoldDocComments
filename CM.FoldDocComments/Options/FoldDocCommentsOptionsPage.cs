using System.ComponentModel;
using System.Runtime.InteropServices;

namespace CM.FoldDocComments
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class FoldDocCommentsOptionsPageOptions : BaseOptionPage<FoldDocCommentsOptionsPage> { }
    }

    public class FoldDocCommentsOptionsPage : BaseOptionModel<FoldDocCommentsOptionsPage>
    {
        [Category("General")]
        [DisplayName("Track Cursor Location")]
        [Description("After comments are collapsed or expanded the editor should scroll to the cursor location.")]
        [DefaultValue(true)]
        public bool TrackCursorLocation { get; set; } = true;
    }
}
