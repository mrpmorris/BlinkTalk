using System;

namespace BlinkTalk.Typing.Persistence
{
    public class AutoMigratingDatabase : SqliteDatabase
    {
        public AutoMigratingDatabase(string dbName) : base(dbName)
        {
            AutoMigrate();
        }

        public int DateToInt(DateTime date)
        {
            return (date.Year * 10000) + (date.Month * 100) + date.Day;
        }

        public int TodayAsInt()
        {
            return DateToInt(DateTime.UtcNow.Date);
        }

        private void AutoMigrate()
        {
            //EXAMPLE
            //DataTable dbResult = ExecuteQuery("select max(Version) as Version from DBInfo;");
            //int versionNumber = (int)dbResult.Rows[0]["Version"];
            //if (versionNumber < 2)
            //    UpgradeToV2();
            PerformDBMaintenance();
        }

        private void PerformDBMaintenance()
        {
            string sql = "delete from WordSequences where LastUsedDate <= " + DateToInt(DateTime.UtcNow.Date.AddDays(-30));
            ExecuteNonQuery(sql);
        }
    }
}
