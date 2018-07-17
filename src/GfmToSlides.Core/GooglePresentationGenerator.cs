using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Slides.v1;
using Google.Apis.Slides.v1.Data;

namespace GfmToSlides.Core
{
	public sealed class GooglePresentationGenerator
	{
		public GooglePresentationGenerator(SlidesService slidesService)
		{
			m_slidesService = slidesService ?? throw new ArgumentNullException(nameof(slidesService));
		}

		public async Task<string> GeneratePresentationAsync(PresentationData presentationData, CancellationToken cancellationToken)
		{
			Presentation presentation;
			if (presentationData.Id == null)
			{
				var createRequest = m_slidesService.Presentations.Create(new Presentation { Title = presentationData.Title });
				presentation = await createRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
			}
			else
			{
				var getRequest = m_slidesService.Presentations.Get(presentationData.Id);
				presentation = await getRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
			}

			var replaceSlidesRequests = new List<Request>();

			if (presentationData.EraseSlides && presentation.Slides != null)
				replaceSlidesRequests.AddRange(presentation.Slides.Select(x => MakeDeleteObjectRequest(x.ObjectId)));

			replaceSlidesRequests.AddRange(presentationData.Slides
				.Select(x => MakeCreateSlideRequest(slideId: x.Id, layoutId: presentation.GetSlideLayoutId(GfmToSlidesUtility.GetSlideLayoutName(x.Kind)))));

			await m_slidesService.Presentations.BatchUpdate(
				new BatchUpdatePresentationRequest { Requests = replaceSlidesRequests },
				presentation.PresentationId).ExecuteAsync(cancellationToken).ConfigureAwait(false);

			presentation = await m_slidesService.Presentations.Get(presentation.PresentationId)
				.ExecuteAsync(cancellationToken).ConfigureAwait(false);

			var updateSlidesRequests = new List<Request>();

			foreach (var slideData in presentationData.Slides)
			{
				var slide = presentation.Slides.Single(x => x.ObjectId == slideData.Id);

				switch (slideData.Kind)
				{
				case SlideKind.TitleSlide:
					updateSlidesRequests.AddRange(MakeTextRequests(slide, "CENTERED_TITLE", slideData.Title));
					updateSlidesRequests.AddRange(MakeTextRequests(slide, "SUBTITLE", slideData.Subtitle));
					break;

				case SlideKind.SectionHeader:
					break;

				case SlideKind.SectionTitleAndDescription:
					break;

				case SlideKind.TitleAndBody:
					updateSlidesRequests.AddRange(MakeTextRequests(slide, "TITLE", slideData.Title));
					updateSlidesRequests.AddRange(MakeTextRequests(slide, "BODY", slideData.Body));
					break;

				case SlideKind.TitleAndTwoColumns:
					break;

				default:
					throw new InvalidOperationException($"Unsupported slide kind: {slideData.Kind}");
				}
			}

			await m_slidesService.Presentations.BatchUpdate(
				new BatchUpdatePresentationRequest { Requests = updateSlidesRequests },
				presentation.PresentationId).ExecuteAsync(cancellationToken).ConfigureAwait(false);

			return presentation.PresentationId;
		}

		private static Request MakeCreateSlideRequest(string slideId, string layoutId)
		{
			return new Request
			{
				CreateSlide = new CreateSlideRequest
				{
					ObjectId = slideId,
					SlideLayoutReference = new LayoutReference { LayoutId = layoutId },
				}
			};
		}

		private static Request MakeDeleteObjectRequest(string objectId)
		{
			return new Request
			{
				DeleteObject = new DeleteObjectRequest { ObjectId = objectId },
			};
		}

		private static IReadOnlyList<Request> MakeTextRequests(Page slide, string target, ParagraphData paragraph) =>
			MakeTextRequests(slide, target, new[] { paragraph });

		private static IReadOnlyList<Request> MakeTextRequests(Page slide, string target, IReadOnlyList<ParagraphData> paragraphs)
		{
			var textElement = slide.PageElements.Where(x => x.Shape.Placeholder.Type == target).Select(x => x.ObjectId).FirstOrDefault();
			if (textElement == null)
				throw new InvalidOperationException($"Text element '{target}' not found in slide '{slide.ObjectId}' of layout '{slide.LayoutProperties.Name}'.");

			var text = new StringBuilder();
			var spans = new List<Span>();
			var bulletLists = new List<BulletList>();
			var blockquotes = new List<Blockquote>();

			void closeBulletList()
			{
				if (bulletLists.Count != 0 && bulletLists[bulletLists.Count - 1].End == 0)
					bulletLists[bulletLists.Count - 1].End = text.Length;
			}

			void closeBlockquote()
			{
				if (blockquotes.Count != 0 && blockquotes[blockquotes.Count - 1].End == 0)
					blockquotes[blockquotes.Count - 1].End = text.Length;
			}

			bool isFirstParagraph = true;
			foreach (var paragraph in paragraphs)
			{
				if (isFirstParagraph)
					isFirstParagraph = false;
				else
					text.Append("\n");

				if (paragraph.ListItem != null)
				{
					closeBlockquote();

					if (bulletLists.Count == 0 || bulletLists[bulletLists.Count - 1].End != 0)
						bulletLists.Add(new BulletList { Start = text.Length, IsOrdered = paragraph.ListItem.IsOrdered });

					text.Append(new string('\t', paragraph.ListItem.Level + 1));
				}
				else if (paragraph.IsBlockquote)
				{
					closeBulletList();

					if (blockquotes.Count == 0 || blockquotes[bulletLists.Count - 1].End != 0)
						blockquotes.Add(new Blockquote { Start = text.Length });
				}
				else
				{
					closeBulletList();
					closeBlockquote();
				}

				foreach (var run in paragraph.Runs)
				{
					var span = new Span(text.Length, text.Length + run.Text.Length);
					if (run.IsBold)
						span.Formats.Add((x => x.Bold = true, "bold"));
					if (run.IsItalic)
						span.Formats.Add((x => x.Italic = true, "italic"));
					if (run.IsStrikethrough)
						span.Formats.Add((x => x.Strikethrough = true, "strikethrough"));
					if (run.IsCode)
						span.Formats.Add((x => x.FontFamily = "Consolas", "fontFamily"));
					if (run.LinkUrl != null)
						span.Formats.Add((x => x.Link = new Link { Url = run.LinkUrl }, "link"));
					if (span.Formats.Count != 0)
						spans.Add(span);

					text.Append(run.Text);
				}
			}

			closeBulletList();
			closeBlockquote();

			var requests = new List<Request>
			{
				new Request
				{
					InsertText = new InsertTextRequest
					{
						Text = text.ToString(),
						ObjectId = textElement,
					},
				}
			};

			foreach (var span in spans)
			{
				var style = new TextStyle();
				foreach (var format in span.Formats)
					format.UpdateStyle(style);

				requests.Add(new Request
				{
					UpdateTextStyle = new UpdateTextStyleRequest
					{
						TextRange = CreateTextRange(span.Start, span.End),
						Style = style,
						Fields = string.Join(",", span.Formats.Select(x => x.Field)),
						ObjectId = textElement,
					},
				});
			}

			foreach (var blockquote in blockquotes)
			{
				requests.Add(new Request
				{
					UpdateTextStyle = new UpdateTextStyleRequest
					{
						TextRange = CreateTextRange(blockquote.Start, blockquote.End),
						Style = new TextStyle { Italic = true },
						Fields = "italic",
						ObjectId = textElement,
					},
				});
			}

			foreach (var bulletList in bulletLists.OrderByDescending(x => x.Start))
			{
				requests.Add(new Request
				{
					CreateParagraphBullets = new CreateParagraphBulletsRequest
					{
						TextRange = CreateTextRange(bulletList.Start, bulletList.End),
						BulletPreset = bulletList.IsOrdered ? "NUMBERED_DIGIT_ALPHA_ROMAN" : "BULLET_DISC_CIRCLE_SQUARE",
						ObjectId = textElement,
					},
				});
			}

			return requests;
		}

		private static Range CreateTextRange(int start, int end)
		{
			return new Range
			{
				Type = "FIXED_RANGE",
				StartIndex = start,
				EndIndex = end,
			};
		}

		private sealed class Span
		{
			public Span(int start, int end)
			{
				Start = start;
				End = end;
				Formats = new Collection<(Action<TextStyle> UpdateStyle, string Field)>();
			}

			public int Start { get; }

			public int End { get; }

			public Collection<(Action<TextStyle> UpdateStyle, string Field)> Formats { get; }
		}

		private sealed class BulletList
		{
			public int Start { get; set; }

			public int End { get; set; }

			public bool IsOrdered { get; set; }
		}

		private sealed class Blockquote
		{
			public int Start { get; set; }

			public int End { get; set; }
		}

		private readonly SlidesService m_slidesService;
	}
}
