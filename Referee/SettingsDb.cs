using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using NLog;
using QuintetLab.ExchangeEngine.Contracts.CryptoSpot.V1;
using QuintetLab.SQLiteHelper.Common.V1;
using QuintetLab.SQLiteHelperCore.Common.V1;

namespace QuintetLab.MatchingEngine.CryptoSpot.V1.Settings
{
    internal class SettingsDb : SQLiteDB
    {
        #region [Udate versions]
        private static readonly Guid GUID = new Guid("66F07B6C-1580-47B2-BEF3-230ACDFEF8D7");
        protected override Version CurrentVersion { get; } = new Version(0, 0, 1);
        protected override Dictionary<Version, Func<SQLiteConnection, Version>> DbUpdates =>
            new Dictionary<Version, Func<SQLiteConnection, Version>>
            {
                {new Version(), UpdateTo0_0_1},
                {new Version(0, 0, 1), UpdateTo0_0_2}
            };

        /// <summary>
        /// update 0.0.0 -> 0.0.1 
        /// </summary>
        /// <param name="con"></param>
        /// <returns></returns>
        private Version UpdateTo0_0_1(SQLiteConnection con)
        {
            //CreateNewTable(con, TABLE_INSTRUMENTS, typeof(RecInstrument));
            return new Version(0, 0, 1);
        }

        /// <summary>
        /// update 0.0.1 -> 0.0.2 
        /// </summary>
        /// <param name="con"></param>
        /// <returns></returns>
        private Version UpdateTo0_0_2(SQLiteConnection con)
        {
            // base.CreateNewTable(con, TABLE_INSTRUMENTS, typeof(SQLiteTable<RecInstrument>));
            return new Version(0, 0, 2);
        }
        #endregion

        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
        
        #region [Tables]
        
        /// <summary>
        /// Table names
        /// </summary>
        private const string TABLE_INSTRUMENTS = "instruments";
        private readonly SQLiteTable<RecInstrument> TableInstruments;
        
        #endregion
        
        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="scriptPath"></param>
        /// <param name="assembly"></param>
        /// <param name="result"></param>
        internal SettingsDb(string fileName,string scriptPath, Assembly assembly, ref bool result) : base(fileName, scriptPath, assembly, ref result,true, GUID)
        {
            TableInstruments = new SQLiteTable<RecInstrument>(this, TABLE_INSTRUMENTS);
        }

        /// <summary>
        /// Save instruments to base
        /// </summary>
        /// <param name="instrument"></param>
        /// <returns></returns>
        internal bool SaveInstrument(CryptoInstrument instrument)
        {
            return TableInstruments.Insert(Cast<RecInstrument>(instrument));
        }

        private T Cast<T>(object obj)
        {
            var target = typeof(T);
            var x = Activator.CreateInstance(target, false);
            var d = from source in target.GetMembers().ToList()
                where source.MemberType == MemberTypes.Property
                select source;
            var memberInfos = d as MemberInfo[] ?? d.ToArray();
            var members = memberInfos.Where(memberInfo => memberInfos.Select(c => c.Name)
                .ToList().Contains(memberInfo.Name)).ToList();
            foreach (var memberInfo in members)
            {
                var propertyInfo = typeof(T).GetProperty(memberInfo.Name);
                var value = obj.GetType().GetProperty(memberInfo.Name)?.GetValue(obj, null);
                propertyInfo?.SetValue(x, value, null);
            }
            return (T)x;
        }

        public void DeleteAllRecords()
        {
            TableInstruments.DeleteAll();
        }

        public bool UpdateOneRecord(CryptoInstrument instrument)
        {
            return TableInstruments.Update(Cast<RecInstrument>(instrument));
        }

        public bool DeleteOneRecord(long key)
        {
            return TableInstruments.Delete(key);
        }
        internal CryptoInstrument[] GetAllInstruments()
        {

            var res = TableInstruments.GetAll();
            var AllInstruments = new CryptoInstrument[res.Count];
            for (var i = 0; i < res.Count; i++)
            {
              AllInstruments[i] = Cast<CryptoInstrument>(res[i]);
            }
            return AllInstruments;
        }

        #region [Main settings]

        internal MainSettings GetMainSettings()
        {
            var res = MainSettings.Default;

            res.RunCount = ReadParam(nameof(res.RunCount), Convert.ToInt32, res.RunCount);
            return res;
        }

        private string GetParamName(string fieldName)
        {
            return $"MAIN_SETTINGS_{fieldName}";
        }

        private T ReadParam<T>(string fieldName, Func<string, T> convertFunc, T defaultValue)
        {
            try
            {
                var str = Settings.ReadParam(GetParamName(fieldName));
                if (str == null) return defaultValue;
                return convertFunc(str);
            }
            catch
            {
                logger.Error($"Cannot read value of setting: {fieldName}, default value will used ");
                
            }

            return defaultValue;
        }

        internal class MainSettings
        {

            // Run Counter
            public int RunCount { get; set; }
            public static MainSettings Default => new MainSettings
            {
                RunCount = 0,
            };

        }


        internal void SaveSettings(MainSettings mainSettings)
        {
            Settings.WriteParam(GetParamName(nameof(mainSettings.RunCount)),
                Convert.ToString(mainSettings.RunCount));
        }
        #endregion

    }
}
