namespace GfmToSlides.Core
{
	public sealed class RunData
	{
		public string Text { get; set; }

		public bool IsBold { get; set; }

		public bool IsItalic { get; set; }

		public bool IsStrikethrough { get; set; }

		public bool IsCode { get; set; }

		public string LinkUrl { get; set; }

		public bool TryMerge(RunData run)
		{
			if (IsBold == run.IsBold &&
				IsItalic == run.IsItalic &&
				IsStrikethrough == run.IsStrikethrough &&
				IsCode == run.IsCode &&
				LinkUrl == run.LinkUrl)
			{
				Text += run.Text;
				return true;
			}

			return false;
		}
	}
}
