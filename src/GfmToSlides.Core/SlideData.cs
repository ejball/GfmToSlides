using System;
using System.Collections.Generic;

namespace GfmToSlides.Core
{
	public sealed class SlideData
	{
		public string Id { get; set; }

		public SlideKind Kind { get; set; }

		public ParagraphData Title { get; set; }

		public ParagraphData Subtitle { get; set; }

		public IReadOnlyList<ParagraphData> Body { get; set; }
	}
}
