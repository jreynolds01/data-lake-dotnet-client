﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ADL_Client_Tests.Store
{
    [TestClass]
    public class FsUnixTime_Tests : Base_Tests
    {
        [TestMethod]
        public void FsUnixTime_Constructor_Default()
        {
            var ut0 = new AzureDataLakeClient.Store.FsUnixTime();
            Assert.AreEqual(0, ut0.MillisecondsSinceEpoch);

            var dt0 = ut0.ToToDateTimeOffset();

            Assert.AreEqual(1970, dt0.Year);
            Assert.AreEqual(1, dt0.Month);
            Assert.AreEqual(1, dt0.Day);
            Assert.AreEqual(0, dt0.Hour);
            Assert.AreEqual(0, dt0.Minute);
            Assert.AreEqual(0, dt0.Second);

            var ut1 = new AzureDataLakeClient.Store.FsUnixTime(dt0);
            Assert.AreEqual(0, ut1.MillisecondsSinceEpoch);
        }

        [TestMethod]
        public void FsUnixTime_Constructor_SpecificDate()
        {
            var d0 = new System.DateTimeOffset(2016,3,31,1,2,3,TimeSpan.Zero);
            var ut0 = new AzureDataLakeClient.Store.FsUnixTime(d0);
            var d1 = ut0.ToToDateTimeOffset();
            Assert.AreEqual(2016, d1.Year);
            Assert.AreEqual(3, d1.Month);
            Assert.AreEqual(31, d1.Day);
            Assert.AreEqual(1, d1.Hour);
            Assert.AreEqual(2, d1.Minute);
            Assert.AreEqual(3, d1.Second);
        }


        [TestMethod]
        public void FsUnixTime_Move_One_Hour_ahead_from_epoch()
        {
            var ut0 = new AzureDataLakeClient.Store.FsUnixTime();
            var ut1 = new AzureDataLakeClient.Store.FsUnixTime(60*60*1000);

            Assert.AreEqual((60 * 60 *1000), ut1.MillisecondsSinceEpoch - ut0.MillisecondsSinceEpoch);

            var d2 = ut1.ToToDateTimeOffset();
            Assert.AreEqual(1970, d2.Year);
            Assert.AreEqual(1, d2.Month);
            Assert.AreEqual(1, d2.Day);
            Assert.AreEqual(1, d2.Hour);
            Assert.AreEqual(0, d2.Minute);
            Assert.AreEqual(0, d2.Second);


        }


        [TestMethod]
        public void FsUnixTime_Move_One_Hour_ahead()
        {
            var d0 = new System.DateTimeOffset(2016, 3, 31, 1, 2, 3, TimeSpan.Zero);
            var d1 = d0.AddSeconds(60*60);

            var ut0 = new AzureDataLakeClient.Store.FsUnixTime(d0);
            var ut1 = new AzureDataLakeClient.Store.FsUnixTime(d1);

            Assert.AreEqual( (60*60*1000), ut1.MillisecondsSinceEpoch - ut0.MillisecondsSinceEpoch);

            var d2 = ut1.ToToDateTimeOffset();
            Assert.AreEqual(2016, d2.Year);
            Assert.AreEqual(3, d2.Month);
            Assert.AreEqual(31, d2.Day);
            Assert.AreEqual(2, d2.Hour);
            Assert.AreEqual(2, d2.Minute);
            Assert.AreEqual(3, d2.Second);


        }

    }
}
