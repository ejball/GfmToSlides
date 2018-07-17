using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Slides.v1.Data;
using Markdig.Syntax;

namespace GfmToSlides.Core
{
	internal static class GfmToSlidesUtility
	{
		public static IEnumerable<ContainerBlock> GetParents(this Block block)
		{
			var parent = block.Parent;
			while (parent != null)
			{
				yield return parent;
				parent = parent.Parent;
			}
		}

		public static string GetSlideLayoutName(SlideKind kind)
		{
			switch (kind)
			{
			case SlideKind.TitleSlide:
				return "TITLE";
			case SlideKind.SectionHeader:
				return "SECTION_HEADER";
			case SlideKind.SectionTitleAndDescription:
				return "SECTION_TITLE_AND_DESCRIPTION";
			case SlideKind.TitleAndBody:
				return "TITLE_AND_BODY";
			case SlideKind.TitleAndTwoColumns:
				return "TITLE_AND_TWO_COLUMNS";
			default:
				throw new InvalidOperationException($"Unsupported slide kind '{kind}.");
			}
		}

		public static string GetSlideLayoutId(this Presentation presentation, string layoutName)
		{
			var layout = presentation.Layouts.FirstOrDefault(x => x.LayoutProperties.Name == layoutName);
			if (layout == null)
				throw new InvalidOperationException($"Presentation missing layout '{layoutName}'.");
			return layout.ObjectId;
		}
	}
}
