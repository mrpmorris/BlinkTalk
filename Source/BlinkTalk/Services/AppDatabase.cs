using BlinkTalk.Application.Abstractions;
using BlinkTalk.Application.Persistence;

namespace BlinkTalk.Services;

/// <summary>
/// The app's <see cref="ISqliteDatabase"/>, forwarding to the database of the language the app is
/// currently running in. Each language has its own file (<see cref="MauiDatabaseProvisioner"/>
/// names it after <see cref="AppLanguage.Name"/>) because its dictionary is seeded from that
/// language's word list, so changing the language has to swap the connection underneath the
/// prediction services — they hold this instance for the life of the app.
///
/// Opening is deliberately lazy. The settings page has to be able to change language without a
/// database being touched, so nothing is opened until the first query, or until
/// <see cref="OpenForCurrentLanguage"/> is called once the choice has been made. Migration seeds
/// tens of thousands of words on a first run, which is why it happens at a point the person is
/// expecting to wait rather than on every keystroke of a language change.
/// </summary>
public sealed class AppDatabase : ISqliteDatabase, IDisposable
{
	private readonly IClock Clock;
	// Guards the open/swap. Queries all arrive on the UI thread (the #1 correctness rule), but the
	// swap is cheap to make safe and a half-swapped connection would be a miserable bug to find.
	private readonly Lock SyncRoot = new Lock();
	private MicrosoftDataSqliteDatabase? Open;
	private string? OpenLanguage;
	private readonly IDatabaseProvisioner Provisioner;
	private readonly ISeedWordSource SeedWords;

	public AppDatabase(IDatabaseProvisioner provisioner, IClock clock, ISeedWordSource seedWords)
	{
		Provisioner = provisioner;
		Clock = clock;
		SeedWords = seedWords;
	}

	public void Dispose()
	{
		lock (SyncRoot)
		{
			Open?.Dispose();
			Open = null;
			OpenLanguage = null;
		}
	}

	public int ExecuteNonQuery(string sql, params (string name, object? value)[] parameters) =>
		Current.ExecuteNonQuery(sql, parameters);

	public DataTable ExecuteQuery(string sql, params (string name, object? value)[] parameters) =>
		Current.ExecuteQuery(sql, parameters);

	public object? ExecuteScalar(string sql, params (string name, object? value)[] parameters) =>
		Current.ExecuteScalar(sql, parameters);

	/// <summary>
	/// Opens and migrates the database for the current language, replacing the open one if the
	/// language has changed since. Blocks while it does so.
	/// </summary>
	public void OpenForCurrentLanguage() => OpenCore(SeedWords);

	/// <summary>
	/// As <see cref="OpenForCurrentLanguage()"/>, but seeding from the given source instead of the
	/// registered one — used to seed a freshly downloaded language pack. The override applies to
	/// this open only; it is not remembered.
	/// </summary>
	public void OpenForCurrentLanguage(ISeedWordSource seedOverride) => OpenCore(seedOverride);

	/// <summary>
	/// Closes the open connection (if any) so the next query reopens. Lets the settings page delete
	/// a database file whose creation failed part-way through.
	/// </summary>
	public void CloseCurrent() => Dispose();

	private ISqliteDatabase Current => OpenCore(SeedWords);

	private ISqliteDatabase OpenCore(ISeedWordSource seedWords)
	{
		lock (SyncRoot)
		{
			string language = AppLanguage.Name;
			if (Open is not null && OpenLanguage == language)
				return Open;

			Open?.Dispose();
			var database = new MicrosoftDataSqliteDatabase(Provisioner.GetDatabasePath());
			new AutoMigratingDatabase(database, Clock, seedWords).Migrate();
			Open = database;
			OpenLanguage = language;
			return database;
		}
	}
}
