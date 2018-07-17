using System.Collections.Generic;

namespace GfmToSlides.Core
{
	public sealed class PresentationData
	{
		public string Id { get; set; }

		public string Title { get; set; }

		public IReadOnlyList<SlideData> Slides { get; set; }

		public bool EraseSlides { get; set; }
	}
}
