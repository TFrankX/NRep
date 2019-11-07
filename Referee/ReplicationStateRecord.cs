using System;
using System.Text.Json;
using QuintetLab.SQLiteHelperCore.Common.V1;

namespace QuintetLab.MatchingEngine.CryptoSpot.V1.Settings
{

    public class ReplicationStateRecord : SQLiteRecord
    {
        [SQLiteField(FieldName = "Id", IsPrimaryKey = true, IsNotNull = true, IsUnique = true,
            Comment = "Instrument ID")]
        public long Id { get; set; }

        [SQLiteField(FieldName = "Ticker", IsNotNull = true,
            Comment = "Short unique name of the instrument")]
        public string Ticker { get; set; }

        [SQLiteField(FieldName = "BaseAssetId", IsNotNull = true,
            Comment = "Base asset id of the instrument")]
        public long BaseAssetId { get; set; }

        [SQLiteField(FieldName = "QuoteAssetId", IsNotNull = true,
            Comment = "Quote asset id of the instrument")]
        public long QuoteAssetId { get; set; }

        [SQLiteField(FieldName = "DefaultFeeAssetId", IsNotNull = true,
            Comment = "Id of the asset in which the fee is taken by default")]
        public long DefaultFeeAssetId { get; set; }


       

        [SQLiteField(FieldName = "TickSize", IsNotNull = true,
            Comment = "The minimum price movement of the instrument (the smallest price increment of an order placed for the instrument)")]
        public decimal TickSize { get; set; }

        [SQLiteField(FieldName = "MinPrice", IsNotNull = true,
            Comment = "Minimum price of an order placed for the instrument")]
        public decimal MinPrice { get; set; }


        [SQLiteField(FieldName = "MaxPrice", IsNotNull = true,
            Comment = "Maximum price of an order placed for the instrument")]
        public decimal MaxPrice { get; set; }

        [SQLiteField(FieldName = "MinVolumeBase", IsNotNull = true,
            Comment = "Minimum volume (in base asset) of an order")]
        public decimal MinVolumeBase { get; set; }

        [SQLiteField(FieldName = "MaxVolumeBase", IsNotNull = true,
            Comment = "Maximum volume (in base asset) of an order")]
        public decimal MaxVolumeBase { get; set; }

        [SQLiteField(FieldName = "MinVolumeQuote", IsNotNull = true,
            Comment = "Minimum volume in quote asset (order volume * price) of an order")]
        public decimal MinVolumeQuote { get; set; }

        [SQLiteField(FieldName = "MaxVolumeQuote", IsNotNull = true,
            Comment = "Maximum volume in quote asset (order volume * price) of an order")]
        public decimal MaxVolumeQuote { get; set; }

        [SQLiteField(FieldName = "BaseVolumeStep", IsNotNull = true,
            Comment = "The step of order volume")]
        public decimal BaseVolumeStep { get; set; }

        [SQLiteField(FieldName = "CreationTimestamp", IsNotNull = false,
            Comment = "Instrument creation timestamp")]
        public DateTimeOffset CreationTimestamp { get; set; }


        [SQLiteField(FieldName = "Settings", IsNotNull = true,
            Comment = "Short unique name of the instrument")]
        public string SettingsStr { get; set; }

        public JsonElement? Settings
        {
            get => JsonSerializer.Deserialize<JsonElement>(SettingsStr);
            set => SettingsStr = value?.GetRawText();
        }
    }
}

