namespace BlinkTalk.Typing.Persistence
{

    public static class PersistenceService
    {
        public static AutoMigratingDatabase DB { get; private set; }

        public static void Initialize(string language)
        {
            DB = new AutoMigratingDatabase(language + ".db");
        }
    }
}