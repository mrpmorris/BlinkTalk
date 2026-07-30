using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using LanguagePackBuilder;


Console.OutputEncoding = Encoding.UTF8;

string? packsDir = FindLanguagePacksFolder();
if (packsDir is null)
{
	Console.Error.WriteLine("Error: Could not find a \"LanguagePacks\" folder in the current directory or any parent folder.");
	Environment.Exit(1);
}

Dictionary<string, CultureInfo> cultures =
	CultureInfo
	.GetCultures(CultureTypes.SpecificCultures)
	.ToDictionary(x => x.Name, x => x, StringComparer.InvariantCultureIgnoreCase);

string? localeInput;
CultureInfo locale = default(CultureInfo)!;
while (true)
{
	Console.Write("Enter a locale (e.g. en-GB): ");
	localeInput = Console.ReadLine()?.Trim();
	if (!string.IsNullOrWhiteSpace(localeInput) && cultures.TryGetValue(localeInput, out CultureInfo? tmp) && tmp is not null)
	{
		locale = tmp;
		break;
	}
	Console.WriteLine("Not an installed locale.\n");
}

string[] txtFiles =
	Directory
	.GetFiles(packsDir, "*.txt")
	.Select(f => Path.GetFileName(f)!)
	.Where(f => !f.Equals("Alphabetical.txt", StringComparison.OrdinalIgnoreCase) && !f.Equals("Frequency.txt", StringComparison.OrdinalIgnoreCase))
	.OrderBy(n => n)
	.ToArray();

string? txtFile;
while (true)
{
	Console.WriteLine(string.Join(", ", txtFiles));
	Console.Write("Choose a TXT file: ");
	string? filenameInput = Console.ReadLine()?.Trim() ?? "";
	txtFile = txtFiles
		.FirstOrDefault(f =>
			f.Equals(filenameInput, StringComparison.OrdinalIgnoreCase)
			|| f.Equals(filenameInput + ".txt", StringComparison.OrdinalIgnoreCase)
		);
	if (txtFile is not null)
		break;
	Console.WriteLine("File not found.\n");
}

// The app reads a pack as a zip holding one CSV (see WordListZipReader), so the zip is what this
// tool produces - the loose CSV was only ever an intermediate step towards it.
// Named the same for every language: the word list is the part that differs per language, the
// layouts file is read by a person who is transcribing whichever pack they just built.
const string LayoutsName = "KeyboardLayouts.txt";
string csvName = Path.ChangeExtension(txtFile, ".csv");
string zipPath = Path.Combine(packsDir, Path.ChangeExtension(txtFile, ".zip"));
Console.WriteLine($"Process {txtFile} to {Path.GetFileName(zipPath)} using culture {locale.Name}");
Console.Write("Continue? [y/N] ");
string? confirm = Console.ReadLine()?.Trim();
if (confirm != "y" && confirm != "Y")
{
	Console.WriteLine("Cancelled");
	return;
}

string text = File.ReadAllText(Path.Combine(packsDir, txtFile), Encoding.UTF8);

// Get rid of lines that are only numbers
text = Regex.Replace(text, @"^\d+\t\d+\t.*\r?\n", "", RegexOptions.Multiline);
// Get rid of leading line numbers
text = Regex.Replace(text, @"^\d+\t", "", RegexOptions.Multiline);
// Get rid of lines with , in them
text = Regex.Replace(text, @"^.*?,.*\r?\n", "", RegexOptions.Multiline);
// Replace tabs with ,
text = text.Replace("\t", ",");
// Replace weird apostrophes
text = Regex.Replace(text, @"[’`]", "'");
// Remove trailing . from words like Dr. or Mr.
text = Regex.Replace(text, @"\.\,", ",");
// Get rid of anything that has a non-word ' or digit
text = Regex.Replace(text, @"^[^,\r\n]*[^\w',\d\r\n][^,\r\n]*,\d+\r?\n", "", RegexOptions.Multiline);
// Get rid of words that start with a non digit letter and then have a digit
text = Regex.Replace(text, @"^[^,\r\n\d]+\d+[^,\r\n]*,\d+\r?\n", "", RegexOptions.Multiline);
// Get rid of single char entries
text = Regex.Replace(text, @"^.?,\d+\r?\n", "", RegexOptions.Multiline);

string[] lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
Dictionary<string, long> merged = new Dictionary<string, long>();
foreach (string line in lines)
{
	string[] parts = line.Split(',');
	if (parts.Length == 2 && long.TryParse(parts[1].Trim(), out long count))
	{
		string word = parts[0].ToUpper(locale);
		// Remove lines that have characters not valid for the selected locale ( ' is allowed)
		if (!IcuAlphabet.IsValidWord(locale, word.Replace("'", "")))
			continue;
		merged.TryGetValue(word, out long existing);
		merged[word] = existing + count;
	}
}

List<KeyValuePair<string, long>> sorted = merged.OrderByDescending(kv => kv.Value).ToList();

Dictionary<char, long> counts = new Dictionary<char, long>();
bool hasApostrophe = false;
foreach (KeyValuePair<string, long> kv in sorted)
{
	foreach (char ch in kv.Key)
	{
		// ' earns no grid cell of its own; it is appended to the end of the last row instead, and only
		// for a language that writes it. The source is the test because CLDR has no data that answers
		// the question: the letter exemplar sets exclude ' even for English and French, while the
		// punctuation set includes it for languages that never write it inside a word. One sighting is
		// not enough, though — a scraped corpus carries stray quote marks stuck to word edges, and
		// those are singletons, where a language that truly writes the apostrophe repeats it endlessly.
		if (ch == '\'') { hasApostrophe |= kv.Value > 1; continue; }
		counts.TryGetValue(ch, out long cur);
		counts[ch] = cur + kv.Value;
	}
}

// Combining marks are not keys of their own: the app offers them through the decorator popup, so they
// leave the letter grid and are listed on their own at the end. Only the marks the corpus actually
// uses are listed, because every extra one is another item the person has to wait out while scanning.
char[] markOrder = counts.Where(kv => IcuAlphabet.IsCombiningMark(kv.Key)).OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToArray();
char[] freqOrder = counts.Where(kv => !IcuAlphabet.IsCombiningMark(kv.Key)).OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToArray();
StringComparer comp = StringComparer.Create(locale, CompareOptions.IgnoreNonSpace);
char[] alphaOrder = freqOrder.OrderBy(l => l.ToString(), comp).ToArray();

int n = alphaOrder.Length;
int cols = (int)Math.Ceiling(n / 4.0);

string digitsRow = string.Join(",", IcuAlphabet.GetDigits(locale));

// The layouts go into the pack as well as onto the console, because they are what gets transcribed
// into a LanguageKeyboard and there is no point rebuilding a whole corpus to read them back.
StringBuilder layouts = new StringBuilder();
layouts.AppendLine("# Layouts");
layouts.AppendLine();
layouts.AppendLine("## Alphabetical");
for (int r = 0; r < 4; r++)
{
	List<string> row = alphaOrder.Skip(r * cols).Take(cols).Select(c => c.ToString()).ToList();
	if (r == 3 && hasApostrophe) row.Add("'");
	layouts.AppendLine(string.Join(",", row));
}
layouts.AppendLine(digitsRow);

List<(int x, int y)> cells = new List<(int x, int y)>();
for (int y = 0; y < 4; y++)
	for (int x = 0; x < cols; x++)
		cells.Add((x, y));

cells.Sort((a, b) =>
{
	double da = Math.Sqrt(a.x * a.x + a.y * a.y);
	double db = Math.Sqrt(b.x * b.x + b.y * b.y);
	int cmp = da.CompareTo(db);
	return cmp != 0 ? cmp : a.y.CompareTo(b.y);
});

Dictionary<(int, int), char> grid = new Dictionary<(int, int), char>();
for (int i = 0; i < cells.Count && i < freqOrder.Length; i++)
	grid[cells[i]] = freqOrder[i];

layouts.AppendLine();
layouts.AppendLine("## Speed");
for (int y = 0; y < 4; y++)
{
	List<string> row = new List<string>();
	for (int x = 0; x < cols; x++)
		if (grid.TryGetValue((x, y), out char ch)) row.Add(ch.ToString());
	if (y == 3 && hasApostrophe) row.Add("'");
	layouts.AppendLine(string.Join(",", row));
}
layouts.AppendLine(digitsRow);

// Most used first, so the order can be transcribed straight into a LanguageKeyboard's Decorators,
// where the first mark is the one the decorator scan reaches soonest. The section is left out
// entirely for a language whose corpus writes no marks, rather than left standing empty.
if (markOrder.Length > 0)
{
	layouts.AppendLine();
	layouts.AppendLine("# Decorators");
	foreach (char mark in markOrder)
		layouts.AppendLine(mark.ToString());
}

Console.WriteLine();
Console.Write(layouts.ToString());

// Deleting first because both writers below append to an existing archive rather than replace it,
// and a rebuilt language must end up with one copy of each file in its pack, not two.
if (File.Exists(zipPath))
	File.Delete(zipPath);

// The pack contents are staged outside the packs folder: they are the input to the compressor, not
// outputs anybody wants left behind next to the pack.
string stagingDir = Directory.CreateTempSubdirectory("LanguagePackBuilder").FullName;
try
{
	using (StreamWriter w = new StreamWriter(Path.Combine(stagingDir, csvName), false, Encoding.UTF8))
	{
		foreach (KeyValuePair<string, long> kv in sorted)
			w.WriteLine($"{kv.Key},{kv.Value}");
	}
	File.WriteAllText(Path.Combine(stagingDir, LayoutsName), layouts.ToString(), Encoding.UTF8);

	// 7-Zip's deflate encoder beats the framework's strongest setting by around 15% on these word
	// lists, and a pack is downloaded over whatever connection the person has, so it is worth
	// shelling out for when installed. The fallback still writes a valid pack, only a fatter one.
	string? sevenZip = FindSevenZip();
	if (sevenZip is not null)
	{
		Compress7Zip(sevenZip, stagingDir, zipPath, [csvName, LayoutsName]);
	}
	else
	{
		Console.WriteLine();
		Console.WriteLine("7-Zip not found - using the built-in deflate, which makes a noticeably larger pack.");
		using ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
		foreach (string name in new[] { csvName, LayoutsName })
		{
			using Stream entry = archive.CreateEntry(name, CompressionLevel.SmallestSize).Open();
			using FileStream staged = File.OpenRead(Path.Combine(stagingDir, name));
			staged.CopyTo(entry);
		}
	}
}
finally
{
	Directory.Delete(stagingDir, true);
}

Console.WriteLine();
Console.WriteLine($"Done. Wrote {zipPath} ({new FileInfo(zipPath).Length:N0} bytes)");


static string? FindSevenZip()
{
	IEnumerable<string> onPath =
		(Environment.GetEnvironmentVariable("PATH") ?? "")
		.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
		.SelectMany(dir => new[] { Path.Combine(dir, "7z.exe"), Path.Combine(dir, "7z") });

	// 7-Zip does not put itself on the PATH when it installs, so the default install folders are
	// checked too - otherwise a machine that has it would silently get the weaker fallback.
	string[] wellKnown =
	[
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.exe"),
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "7-Zip", "7z.exe"),
		"/usr/bin/7z",
		"/usr/local/bin/7z",
	];

	return onPath.Concat(wellKnown).FirstOrDefault(File.Exists);
}

static void Compress7Zip(string sevenZipPath, string workingDirectory, string zipPath, IEnumerable<string> fileNames)
{
	// -tzip because the pack has to stay readable by ZipArchive, which rules out the stronger LZMA
	// method 7-Zip would reach for; -mx=9 is the "Ultra" level of the compression dialog.
	ProcessStartInfo info = new ProcessStartInfo(sevenZipPath)
	{
		WorkingDirectory = workingDirectory,
		RedirectStandardOutput = true,
		RedirectStandardError = true,
	};
	info.ArgumentList.Add("a");
	info.ArgumentList.Add("-tzip");
	info.ArgumentList.Add("-mx=9");
	info.ArgumentList.Add(zipPath);
	// Names only, resolved against the staging folder, so the entries are "English.csv" and
	// "KeyboardLayouts.txt" rather than paths the app's reader would have to look inside folders for.
	foreach (string fileName in fileNames)
		info.ArgumentList.Add(fileName);

	using Process process = Process.Start(info) ?? throw new InvalidOperationException($"Could not start {sevenZipPath}.");
	string output = process.StandardOutput.ReadToEnd();
	string error = process.StandardError.ReadToEnd();
	process.WaitForExit();
	if (process.ExitCode != 0)
		throw new InvalidOperationException($"7-Zip exited with code {process.ExitCode}.{Environment.NewLine}{output}{Environment.NewLine}{error}");
}

static string? FindLanguagePacksFolder()
{
	DirectoryInfo? dir = new DirectoryInfo(Directory.GetCurrentDirectory());
	while (dir is not null)
	{
		string candidate = Path.Combine(dir.FullName, "LanguagePacks");
		if (Directory.Exists(candidate))
			return candidate;
		dir = dir.Parent;
	}
	return null;
}
