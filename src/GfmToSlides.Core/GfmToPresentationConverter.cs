using System;
using System.Collections.Generic;
using System.Linq;
using Markdig;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace GfmToSlides.Core
{
	/// <summary>
	/// Converts GitHub Flavored Markdown to information about a Google Slides presentation.
	/// </summary>
	public sealed class GfmToPresentationConverter
	{
		/// <summary>
		/// Converts GitHub Flavored Markdown to information about a Google Slides presentation.
		/// </summary>
		public PresentationData Convert(string markdown)
		{
			var pipeline = new MarkdownPipelineBuilder()
				.UseAutoLinks()
				.UseEmojiAndSmiley(enableSmiley: false)
				.UseEmphasisExtras(EmphasisExtraOptions.Strikethrough)
				.UsePipeTables(new PipeTableOptions { RequireHeaderSeparator = true })
				.Build();
			var document = Markdown.Parse(markdown, pipeline);

			var leafBlocks = document.Descendants().OfType<LeafBlock>().ToList();
			int leafBlockIndex = 0;
			Block getNextBlock() => leafBlockIndex == leafBlocks.Count ? null : leafBlocks[leafBlockIndex++];

			// ignore everything before first heading
			var block = getNextBlock();
			while (block != null)
			{
				if (block is HeadingBlock)
					break;

				block = getNextBlock();
			}

			var slides = new List<SlideData>();
			SlideData slide = null;
			var body = new List<ParagraphData>();

			void addToBody(ParagraphData paragraph)
			{
				if (slide.Body == null)
					slide.Body = body = new List<ParagraphData>();
				body.Add(paragraph);
			}

			while (block != null)
			{
				if (block is HeadingBlock heading)
				{
					slide = new SlideData
					{
						Id = CreateId(),
						Kind = GetSlideKindFromHeadingLevel(heading.Level),
						Title = GetParagraph(heading),
					};
					slides.Add(slide);
				}
				else if (block is ParagraphBlock paragraph)
				{
					if (slide.Body == null &&
						(slide.Kind == SlideKind.TitleSlide || slide.Kind == SlideKind.SectionHeader) &&
						paragraph.Inline.Count() == 1 &&
						paragraph.Inline.Single() is EmphasisInline emphasis &&
						(IsBold(emphasis) || IsItalic(emphasis)))
					{
						if (slide.Kind == SlideKind.SectionHeader)
							slide.Kind = SlideKind.SectionTitleAndDescription;

						slide.Subtitle = GetParagraph(paragraph);

						foreach (var run in slide.Subtitle.Runs)
						{
							if (IsBold(emphasis))
								run.IsBold = false;
							else
								run.IsItalic = false;
						}
					}
					else
					{
						if (slide.Kind == SlideKind.SectionHeader)
							slide.Kind = SlideKind.SectionTitleAndDescription;

						if (slide.Kind != SlideKind.SectionTitleAndDescription &&
							slide.Kind != SlideKind.TitleAndBody &&
							slide.Kind != SlideKind.TitleAndTwoColumns)
						{
							slide = new SlideData
							{
								Id = CreateId(),
								Kind = SlideKind.TitleAndBody,
								Title = slide.Title,
							};
							slides.Add(slide);
						}

						addToBody(GetParagraph(paragraph));
					}
				}
				else if (block is CodeBlock code)
				{
					if (slide.Kind != SlideKind.TitleAndBody &&
						slide.Kind != SlideKind.TitleAndTwoColumns)
					{
						slide = new SlideData
						{
							Id = CreateId(),
							Kind = SlideKind.TitleAndBody,
							Title = slide.Title,
						};
						slides.Add(slide);
					}

					addToBody(GetParagraph(code));
				}
				else if (block is ThematicBreakBlock)
				{
					// ignore everything after horizontal line
					break;
				}
				else if (block is HtmlBlock)
				{
					// ignore HTML for now
				}
				else if (block is LinkReferenceDefinition)
				{
				}
				else
				{
					throw new InvalidOperationException($"Unhandled Markdown block type: {block.GetType()}");
				}

				block = getNextBlock();
			}

			if (slides.Count == 0)
				return null;

			return new PresentationData
			{
				Title = slides.Select(x => x.Title).Where(x => x != null).Select(x => string.Concat(x.Runs.Select(r => r.Text))).FirstOrDefault(),
				Slides = slides,
			};
		}

		private static string CreateId() => Guid.NewGuid().ToString("N");

		private static SlideKind GetSlideKindFromHeadingLevel(int level)
		{
			switch (level)
			{
			case 1:
				return SlideKind.TitleSlide;
			case 2:
				return SlideKind.SectionHeader;
			default:
				return SlideKind.TitleAndBody;
			}
		}

		private static ParagraphData GetParagraph(LeafBlock block)
		{
			if (block is CodeBlock code)
			{
				return new ParagraphData
				{
					Runs = new[]
					{
						new RunData
						{
							Text = string.Join("\v", code.Lines.Lines.Select(x => x.ToString())).TrimEnd('\v'),
							IsCode = true,
						}
					},
				};
			}
			else
			{
				return new ParagraphData
				{
					Runs = GetRuns(block.Inline),
					ListItem = GetListItemData(block),
					IsBlockquote = block.GetParents().OfType<QuoteBlock>().Any(),
				};
			}
		}

		private static ListItemData GetListItemData(LeafBlock block)
		{
			ListItemData data = null;

			foreach (var list in block.GetParents().OfType<ListBlock>())
			{
				if (data == null)
					data = new ListItemData();
				else
					data.Level += 1;

				// Google Slides doesn't support mixed ordered/unordered
				if (list.IsOrdered)
					data.IsOrdered = true;
			}

			return data;
		}

		private static IReadOnlyList<RunData> GetRuns(ContainerInline inlines)
		{
			var runs = new List<RunData>();

			foreach (var inline in inlines.Descendants().OfType<LeafInline>())
			{
				string text;

				if (inline is LiteralInline literal)
					text = literal.Content.ToString();
				else if (inline is CodeInline code)
					text = code.Content;
				else if (inline is LineBreakInline lineBreak)
					text = lineBreak.IsHard || lineBreak.IsBackslash ? "\v" : " ";
				else if (inline is HtmlInline html)
					text = null;
				else if (inline is HtmlEntityInline htmlEntity)
					text = htmlEntity.Transcoded.Text;
				else if (inline is AutolinkInline autolink)
					text = autolink.Url;
				else
					throw new InvalidOperationException($"Unhandled Markdown inline type: {inline.GetType()}");

				if (text != null)
				{
					AddRun(runs, new RunData
					{
						Text = text,
						IsBold = inline.FindParentOfType<EmphasisInline>().Any(IsBold),
						IsItalic = inline.FindParentOfType<EmphasisInline>().Any(IsItalic),
						IsStrikethrough = inline.FindParentOfType<EmphasisInline>().Any(IsStrikethrough),
						IsCode = inline is CodeInline,
						LinkUrl = GetLinkUrl(inline),
					});
				}
			}

			return runs;
		}

		private static string GetLinkUrl(LeafInline inline)
		{
			if (inline is AutolinkInline autolink)
				return autolink.IsEmail ? $"mailto:{autolink.Url}" : autolink.Url;

			return inline.FindParentOfType<LinkInline>().Where(x => !x.IsImage).Select(x => x.GetDynamicUrl?.Invoke() ?? x.Url).FirstOrDefault();
		}

		private static void AddRun(IList<RunData> runs, RunData run)
		{
			if (runs.Count == 0 || !runs[runs.Count - 1].TryMerge(run))
				runs.Add(run);
		}

		private static bool IsBold(EmphasisInline x) => (x.DelimiterChar == '*' || x.DelimiterChar == '_') && x.IsDouble;

		private static bool IsItalic(EmphasisInline x) => (x.DelimiterChar == '*' || x.DelimiterChar == '_') && !x.IsDouble;

		private static bool IsStrikethrough(EmphasisInline x) => x.DelimiterChar == '~' && x.IsDouble;
	}
}

