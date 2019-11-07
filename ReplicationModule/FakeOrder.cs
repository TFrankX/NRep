using System;
using System.Collections.Generic;
using System.Text;
using QuintetLab.ExchangeEngine.Contracts.CryptoSpot.V1;
using QuintetLab.MatchingEngine.Contracts.CryptoSpot.V1;
using System.Text.Json;

namespace ReplicationModule
{
    public class FakeOrder
    {
        public CryptoOrder GetNew()
        {
            var rand = new Random();
            CryptoOrder RandOrder = new CryptoOrder
            {
                InstrumentId = rand.Next(1, 100),
                ChildOrderId = rand.Next(1, 1000),
                CreationTimestamp = new DateTimeOffset(rand.Next(2015, 2019), rand.Next(1, 12), rand.Next(1, 28), rand.Next(0, 23), rand.Next(0, 59), rand.Next(0, 59), TimeSpan.Zero),
                LastUpdateTimestamp = new DateTimeOffset(rand.Next(2015, 2019), rand.Next(1, 12), rand.Next(1, 28), rand.Next(0, 23), rand.Next(0, 59), rand.Next(0, 59), TimeSpan.Zero),
                OrderType = rand.Next(0,3)==0 ? CryptoOrderType.Unknown:(rand.Next(0, 2) == 0 ? CryptoOrderType.Limit: rand.Next(0, 1) == 0 ? CryptoOrderType.Market: CryptoOrderType.StopLimit),
                ParentOrderId = rand.Next(0, 1000),
                Price = Convert.ToDecimal(0.01 + (rand.NextDouble() * (10000 - 0.01))),
                RemainingVolume = Convert.ToDecimal(0.01 + (rand.NextDouble() * (10000 - 0.01))),
                Settings = new JsonElement(),
                Status = 0,
            };
            return RandOrder;
        }

        public List<CryptoOrder> Generate(long msgCount)
        {
            List<CryptoOrder> FakeList = new List<CryptoOrder>();

            for (int i = 0; i < msgCount; i++)
            {
                FakeList.Add(GetNew());
            }
            return FakeList;
        }

    }
}
