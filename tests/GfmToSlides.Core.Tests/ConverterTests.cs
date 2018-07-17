using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace GfmToSlides.Core.Tests
{
	public sealed class ConverterTests
	{
		[Test]
		public void EmptyMarkdown()
		{
			Convert("").Should().BeNull();
		}

		[Test]
		public void TitleSlide_Title()
		{
			var presentation = Convert("# The Title");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("The Title",
					CreateSlide(SlideKind.TitleSlide,
						title: CreateParagraph(CreateRun("The Title")))));
		}

		[Test]
		public void TitleSlide_FormattedTitle()
		{
			var presentation = Convert("# The **Title**");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("The Title",
					CreateSlide(SlideKind.TitleSlide,
						title: CreateParagraph(CreateRun("The "), CreateRun("Title", isBold: true)))));
		}

		[Test]
		public void TitleSlide_SubtitleFromItalic()
		{
			var presentation = Convert(@"
# The Title

*The subtitle.*
");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("The Title",
					CreateSlide(SlideKind.TitleSlide,
						title: CreateParagraph(CreateRun("The Title")),
						subtitle: CreateParagraph(CreateRun("The subtitle.")))));
		}

		[Test]
		public void TitleSlide_SubtitleFromBold()
		{
			var presentation = Convert(@"
# The Title

**The subtitle.**
");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("The Title",
					CreateSlide(SlideKind.TitleSlide,
						title: CreateParagraph(CreateRun("The Title")),
						subtitle: CreateParagraph(CreateRun("The subtitle.")))));
		}

		[Test]
		public void TitleSlide_SubtitleFromBoldWithItalic()
		{
			var presentation = Convert(@"
# The Title

**The *subtitle*.**
");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("The Title",
					CreateSlide(SlideKind.TitleSlide,
						title: CreateParagraph(CreateRun("The Title")),
						subtitle: CreateParagraph(CreateRun("The "), CreateRun("subtitle", isItalic: true), CreateRun(".")))));
		}

		[Test]
		public void TitleSlide_BodyOnNextSlide()
		{
			var presentation = Convert(@"
# The Title

The body.
");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("The Title",
					CreateSlide(SlideKind.TitleSlide,
						title: CreateParagraph(CreateRun("The Title"))),
					CreateSlide(SlideKind.TitleAndBody,
						title: CreateParagraph(CreateRun("The Title")), body: CreateBody(CreateParagraph(CreateRun("The body."))))));
		}

		[Test]
		public void TitleSlide_CodeOnNextSlide()
		{
			var presentation = Convert(@"
# The Title

```
The code
is here.
```
");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("The Title",
					CreateSlide(SlideKind.TitleSlide,
						title: CreateParagraph(CreateRun("The Title"))),
					CreateSlide(SlideKind.TitleAndBody,
						title: CreateParagraph(CreateRun("The Title")), body: CreateBody(CreateParagraph(CreateRun("The code\vis here.", isCode: true))))));
		}

		[Test]
		public void Section_Title()
		{
			var presentation = Convert("## The Title");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("The Title",
					CreateSlide(SlideKind.SectionHeader,
						title: CreateParagraph(CreateRun("The Title")))));
		}

		[Test]
		public void Section_Subtitle()
		{
			var presentation = Convert(@"
## The Title

**The subtitle.**
");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("The Title",
					CreateSlide(SlideKind.SectionTitleAndDescription,
						title: CreateParagraph(CreateRun("The Title")),
						subtitle: CreateParagraph(CreateRun("The subtitle.")))));
		}

		[Test]
		public void Section_Body()
		{
			var presentation = Convert(@"
## The Title

The body.
");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("The Title",
					CreateSlide(SlideKind.SectionTitleAndDescription,
						title: CreateParagraph(CreateRun("The Title")),
						body: CreateBody(CreateParagraph(CreateRun("The body."))))));
		}

		[Test]
		public void Section_SubtitleAndBody()
		{
			var presentation = Convert(@"
## The Title

*The subtitle.*

The body.
");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("The Title",
					CreateSlide(SlideKind.SectionTitleAndDescription,
						title: CreateParagraph(CreateRun("The Title")),
						subtitle: CreateParagraph(CreateRun("The subtitle.")),
						body: CreateBody(CreateParagraph(CreateRun("The body."))))));
		}

		[Test]
		public void TitleBody_Title()
		{
			var presentation = Convert("### The Title");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("The Title",
					CreateSlide(SlideKind.TitleAndBody,
						title: CreateParagraph(CreateRun("The Title")))));
		}

		[Test]
		public void TitleBody_Body()
		{
			var presentation = Convert(@"
### The Title

The body.");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("The Title",
					CreateSlide(SlideKind.TitleAndBody,
						title: CreateParagraph(CreateRun("The Title")),
						body: CreateBody(CreateParagraph(CreateRun("The body."))))));
		}

		[Test]
		public void BulletedList()
		{
			var presentation = Convert(@"
### List

* bullet 1
  * bullet 2
* bullet 3");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("List",
					CreateSlide(SlideKind.TitleAndBody,
						title: CreateParagraph(CreateRun("List")),
						body: CreateBody(
							CreateBulletedItem(CreateRun("bullet 1")),
							CreateIndentedBulletedItem(CreateRun("bullet 2")),
							CreateBulletedItem(CreateRun("bullet 3"))))));
		}

		[Test]
		public void NumberedList()
		{
			var presentation = Convert(@"
### List

1. number 1
   1. number 2
1. number 3");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("List",
					CreateSlide(SlideKind.TitleAndBody,
						title: CreateParagraph(CreateRun("List")),
						body: CreateBody(
							CreateNumberedItem(CreateRun("number 1")),
							CreateIndentedNumberedItem(CreateRun("number 2")),
							CreateNumberedItem(CreateRun("number 3"))))));
		}

		[Test]
		public void TitleBody_Formatting()
		{
			var presentation = Convert(@"
### The Title

* **bold**
* *italic*
* **bold *and* italic**
* ~~strikethrough~~
* `code`");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("The Title",
					CreateSlide(SlideKind.TitleAndBody,
						title: CreateParagraph(CreateRun("The Title")),
						body: CreateBody(
							CreateBulletedItem(CreateRun("bold", isBold: true)),
							CreateBulletedItem(CreateRun("italic", isItalic: true)),
							CreateBulletedItem(CreateRun("bold ", isBold: true), CreateRun("and", isBold: true, isItalic: true), CreateRun(" italic", isBold: true)),
							CreateBulletedItem(CreateRun("strikethrough", isStrikethrough: true)),
							CreateBulletedItem(CreateRun("code", isCode: true))))));
		}

		[Test]
		public void TitleBody_Links()
		{
			var presentation = Convert(@"
### The Title

The [GFM](https://guides.github.com/features/mastering-markdown/) spec: https://github.github.com/gfm/");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("The Title",
					CreateSlide(SlideKind.TitleAndBody,
						title: CreateParagraph(CreateRun("The Title")),
						body: CreateBody(CreateParagraph(
							CreateRun("The "),
							CreateRun("GFM", linkUrl: "https://guides.github.com/features/mastering-markdown/"),
							CreateRun(" spec: "),
							CreateRun("https://github.github.com/gfm/", linkUrl: "https://github.github.com/gfm/"))))));
		}

		[Test]
		public void TitleBody_LinkReference()
		{
			var presentation = Convert(@"
### The Title

[GFM]

[GFM]: https://guides.github.com/features/mastering-markdown/
");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("The Title",
					CreateSlide(SlideKind.TitleAndBody,
						title: CreateParagraph(CreateRun("The Title")),
						body: CreateBody(CreateParagraph(
							CreateRun("GFM", linkUrl: "https://guides.github.com/features/mastering-markdown/"))))));
		}

		[Test]
		public void TitleBody_AngleBracketLinks()
		{
			var presentation = Convert(@"
### The Title

<gfm@example.com>

<http://example.com/>
");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("The Title",
					CreateSlide(SlideKind.TitleAndBody,
						title: CreateParagraph(CreateRun("The Title")),
						body: CreateBody(
							CreateParagraph(CreateRun("gfm@example.com", linkUrl: "mailto:gfm@example.com")),
							CreateParagraph(CreateRun("http://example.com/", linkUrl: "http://example.com/"))))));
		}

		[Test]
		public void TitleBody_HtmlEntity()
		{
			var presentation = Convert(@"
### The Title

&lt;happy&gt;hi &amp; bye&lt;/happy&gt;
");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("The Title",
					CreateSlide(SlideKind.TitleAndBody,
						title: CreateParagraph(CreateRun("The Title")),
						body: CreateBody(CreateParagraph(CreateRun("<happy>hi & bye</happy>"))))));
		}

		[Test]
		public void TitleBody_Image()
		{
			var presentation = Convert(@"
### The Title

the ![image](http://example.com/image.png)");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("The Title",
					CreateSlide(SlideKind.TitleAndBody,
						title: CreateParagraph(CreateRun("The Title")),
						body: CreateBody(CreateParagraph(
							CreateRun("the image"))))));
		}

		[Test]
		public void TitleBody_HardLineBreak()
		{
			var presentation = Convert(@"
### The Title

one\
two");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("The Title",
					CreateSlide(SlideKind.TitleAndBody,
						title: CreateParagraph(CreateRun("The Title")),
						body: CreateBody(
							CreateParagraph(CreateRun("one\vtwo"))))));
		}

		[Test]
		public void TitleBody_Emoji()
		{
			var presentation = Convert(@"
### The Title

:sparkles: :boom: :tada: :-)");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("The Title",
					CreateSlide(SlideKind.TitleAndBody,
						title: CreateParagraph(CreateRun("The Title")),
						body: CreateBody(CreateParagraph(CreateRun("✨ 💥 🎉 :-)"))))));
		}

		[Test]
		public void TaskList()
		{
			var presentation = Convert(@"
### List

- [ ] item 1
- [X] item 2
- [ ] item 3");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("List",
					CreateSlide(SlideKind.TitleAndBody,
						title: CreateParagraph(CreateRun("List")),
						body: CreateBody(
							CreateBulletedItem(CreateRun("[ ] item 1")),
							CreateBulletedItem(CreateRun("[X] item 2")),
							CreateBulletedItem(CreateRun("[ ] item 3"))))));
		}

		[Test]
		public void Image_NotYetSupported()
		{
			Convert(@"
### List

![text](http://example.com/image.png)");
		}

		[Test]
		public void Table_NotYetSupported()
		{
			Convert(@"
### List

| head 1 | head 2 |
| --- | --- |
| body 1 | body 2 |");
		}

		[Test]
		public void Comment_Ignored()
		{
			Convert(@"
### List

<!-- ignore me -->");
		}

		[Test]
		public void HtmlBlock_Ignored()
		{
			Convert(@"
### List

<div>
ignored
</div>");
		}

		[Test]
		public void HtmlInline_Ignored()
		{
			Convert(@"
### List

<b>ignored</b>");
		}

		[Test]
		public void IgnoreBeforeFirstHeading()
		{
			var presentation = Convert(@"
ignored

# Title");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("Title",
					CreateSlide(SlideKind.TitleSlide,
						title: CreateParagraph(CreateRun("Title")))));
		}

		[Test]
		public void IgnoreAfterHorizontalLine()
		{
			var presentation = Convert(@"
# Title

----

# Ignored
");

			presentation.Should().BeEquivalentTo(
				CreatePresentation("Title",
					CreateSlide(SlideKind.TitleSlide,
						title: CreateParagraph(CreateRun("Title")))));
		}

		private static PresentationData CreatePresentation(string title, params SlideData[] slides)
		{
			return new PresentationData
			{
				Title = title,
				Slides = slides,
			};
		}

		private static SlideData CreateSlide(SlideKind kind, ParagraphData title = null, ParagraphData subtitle = null, IReadOnlyList<ParagraphData> body = null)
		{
			return new SlideData
			{
				Kind = kind,
				Title = title,
				Subtitle = subtitle,
				Body = body,
			};
		}

		private static ParagraphData CreateParagraph(params RunData[] runs)
		{
			return new ParagraphData
			{
				Runs = runs,
			};
		}

		private static ParagraphData CreateBulletedItem(params RunData[] runs) =>
			CreateListItem(isOrdered: false, level: 0, runs: runs);

		private static ParagraphData CreateIndentedBulletedItem(params RunData[] runs) =>
			CreateListItem(isOrdered: false, level: 1, runs: runs);

		private static ParagraphData CreateNumberedItem(params RunData[] runs) =>
			CreateListItem(isOrdered: true, level: 0, runs: runs);

		private static ParagraphData CreateIndentedNumberedItem(params RunData[] runs) =>
			CreateListItem(isOrdered: true, level: 1, runs: runs);

		private static ParagraphData CreateListItem(bool isOrdered, int level, params RunData[] runs)
		{
			return new ParagraphData
			{
				ListItem = new ListItemData { IsOrdered = isOrdered, Level = level },
				Runs = runs,
			};
		}

		private static IReadOnlyList<ParagraphData> CreateBody(params ParagraphData[] paragraphs)
		{
			return paragraphs;
		}

		private static RunData CreateRun(string text,
			bool isBold = false, bool isItalic = false, bool isStrikethrough = false, bool isCode = false, string linkUrl = null)
		{
			return new RunData
			{
				Text = text,
				IsBold = isBold,
				IsItalic = isItalic,
				IsStrikethrough = isStrikethrough,
				IsCode = isCode,
				LinkUrl = linkUrl,
			};
		}

		private static PresentationData Convert(string markdown)
		{
			var presentation = new GfmToPresentationConverter().Convert(markdown);

			if (presentation != null)
			{
				foreach (var slide in presentation.Slides)
				{
					slide.Id.Should().NotBeNullOrWhiteSpace();
					slide.Id = null;
				}
			}

			return presentation;
		}
	}
}
