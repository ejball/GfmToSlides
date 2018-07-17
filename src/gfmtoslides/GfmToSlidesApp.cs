using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArgsReading;
using GfmToSlides.Core;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Slides.v1;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace gfmtoslides
{
	public sealed class GfmToSlidesApp
	{
		public static async Task<int> Main(string[] args)
		{
			var cancellationTokenSource = new CancellationTokenSource();

			Console.CancelKeyPress += (s, e) =>
			{
				e.Cancel = true;
				cancellationTokenSource.Cancel();
			};

			int exitCode = await new GfmToSlidesApp().RunAsync(args, cancellationTokenSource.Token).ConfigureAwait(false);

			if (Debugger.IsAttached)
			{
				Console.WriteLine("Press a key to continue...");
				Console.ReadKey();
			}

			return exitCode;
		}

		public async Task<int> RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken)
		{
			try
			{
				var argsReader = new ArgsReader(args);
				if (argsReader.ReadFlag("help|h|?"))
				{
					WriteUsage(Console.Out);
					return 0;
				}

				string markdownPath = argsReader.ReadArgument();
				if (markdownPath == null)
					throw new ArgsReaderException("Missing Markdown file path.");

				string presentationId = argsReader.ReadOption("id");
				string presentationTitle = argsReader.ReadOption("title");
				if (presentationId != null && presentationTitle != null)
					throw new ArgsReaderException("--id and --title cannot be used together.");

				bool eraseSlides = argsReader.ReadFlag("erase");

				argsReader.VerifyComplete();

				var markdown = await File.ReadAllTextAsync(markdownPath, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
				var converter = new GfmToPresentationConverter();
				var presentation = converter.Convert(markdown);

				if (!string.IsNullOrWhiteSpace(presentationId))
					presentation.Id = presentationId;
				if (!string.IsNullOrWhiteSpace(presentationTitle))
					presentation.Title = presentationTitle;
				presentation.EraseSlides = eraseSlides;

				var slidesService = await CreateSlidesServiceAsync(cancellationToken).ConfigureAwait(false);
				var presentationFactory = new GooglePresentationGenerator(slidesService);
				presentationId = await presentationFactory.GeneratePresentationAsync(presentation, cancellationToken).ConfigureAwait(false);
				Console.WriteLine("https://docs.google.com/presentation/d/{0}/edit", presentationId);

				return 0;
			}
			catch (Exception exception)
			{
				if (exception is ArgsReaderException)
				{
					Console.Error.WriteLine(exception.Message);
					Console.Error.WriteLine();
					WriteUsage(Console.Error);
					return 2;
				}
				else if (exception is ApplicationException || exception is InvalidOperationException ||
					exception is IOException || exception is UnauthorizedAccessException)
				{
					Console.Error.WriteLine(exception.Message);
					return 3;
				}
				else
				{
					Console.Error.WriteLine(exception.ToString());
					return 3;
				}
			}
		}

		private async Task<SlidesService> CreateSlidesServiceAsync(CancellationToken cancellationToken)
		{
			string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "gfmtoslides");

			ClientSecrets secrets;
			using (var stream = new FileStream(Path.Combine(appDataFolder, "client_secret.json"), FileMode.Open, FileAccess.Read))
				secrets = GoogleClientSecrets.Load(stream).Secrets;

			return new SlidesService(new BaseClientService.Initializer
			{
				ApplicationName = "gfmtoslides",
				HttpClientInitializer = await GoogleWebAuthorizationBroker.AuthorizeAsync(
					clientSecrets: secrets,
					scopes: new[] { SlidesService.Scope.Presentations },
					user: "user",
					taskCancellationToken: cancellationToken,
					dataStore: new FileDataStore(Path.Combine(appDataFolder, "user_credential"), fullPath: true)).ConfigureAwait(false),
			});
		}

		private void WriteUsage(TextWriter textWriter)
		{
			textWriter.WriteLine("Creates a Google Slides presentation from GitHub Flavored Markdown.");
			textWriter.WriteLine();
			textWriter.WriteLine("Usage: gfmtoslides <markdown-file> (options)");
			textWriter.WriteLine();
			textWriter.WriteLine("   markdown-file");
			textWriter.WriteLine("      The local or web path to the Markdown file.");
			textWriter.WriteLine();
			textWriter.WriteLine("Options:");
			textWriter.WriteLine();
			textWriter.WriteLine("   --id <presentation-id>");
			textWriter.WriteLine("      The ID of the presentation to which to add slides.");
			textWriter.WriteLine("   --erase");
			textWriter.WriteLine("      Erase all existing slides in the presentation.");
			textWriter.WriteLine("   --title <presentation-title>");
			textWriter.WriteLine("      The title of the presentation (defaults to first slide title).");
		}
	}
}

