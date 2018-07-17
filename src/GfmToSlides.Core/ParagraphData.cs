using System.Collections.Generic;

namespace GfmToSlides.Core
{
	public sealed class ParagraphData
	{
		public IReadOnlyList<RunData> Runs { get; set; }

		public ListItemData ListItem { get; set; }

		public bool IsBlockquote { get; set; }
	}
}
