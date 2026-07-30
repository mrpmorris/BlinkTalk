using System.Globalization;
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

string csvPath = Path.Combine(packsDir, Path.ChangeExtension(txtFile, ".csv"));
Console.WriteLine($"Process {txtFile} to {Path.GetFileName(csvPath)} using culture {locale.Name}");
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
using (StreamWriter w = new StreamWriter(csvPath, false, Encoding.UTF8))
{
	foreach (KeyValuePair<string, long> kv in sorted)
		w.WriteLine($"{kv.Key},{kv.Value}");
}

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

// Combining marks are not keys of their own: the app offers them through the accent popup, so they
// leave the letter grid and are listed on their own at the end. Only the marks the corpus actually
// uses are listed, because every extra one is another item the person has to wait out while scanning.
char[] markOrder = counts.Where(kv => IcuAlphabet.IsCombiningMark(kv.Key)).OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToArray();
char[] freqOrder = counts.Where(kv => !IcuAlphabet.IsCombiningMark(kv.Key)).OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToArray();
StringComparer comp = StringComparer.Create(locale, CompareOptions.IgnoreNonSpace);
char[] alphaOrder = freqOrder.OrderBy(l => l.ToString(), comp).ToArray();

int n = alphaOrder.Length;
int cols = (int)Math.Ceiling(n / 4.0);

string digitsRow = string.Join(",", IcuAlphabet.GetDigits(locale));

Console.WriteLine();
Console.WriteLine("Alphabetical");
Console.WriteLine("=========");
for (int r = 0; r < 4; r++)
{
	List<string> row = alphaOrder.Skip(r * cols).Take(cols).Select(c => c.ToString()).ToList();
	if (r == 3 && hasApostrophe) row.Add("'");
	Console.WriteLine(string.Join(",", row));
}
Console.WriteLine(digitsRow);

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

Console.WriteLine();
Console.WriteLine("Frequency");
Console.WriteLine("========");
for (int y = 0; y < 4; y++)
{
	List<string> row = new List<string>();
	for (int x = 0; x < cols; x++)
		if (grid.TryGetValue((x, y), out char ch)) row.Add(ch.ToString());
	if (y == 3 && hasApostrophe) row.Add("'");
	Console.WriteLine(string.Join(",", row));
}
Console.WriteLine(digitsRow);

// Most used first, so the order can be transcribed straight into AccentScheme, where the first mark
// is the one the accent scan reaches soonest.
if (markOrder.Length > 0)
{
	Console.WriteLine();
	Console.WriteLine("Decorators");
	Console.WriteLine("==========");
	foreach (char mark in markOrder)
		Console.WriteLine(mark);
}

Console.WriteLine("Done.");


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
