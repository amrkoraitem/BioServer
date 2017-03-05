using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Client;
namespace ClientTest
{
    [TestClass]
    public class ClientUnitTest
    {
        [TestMethod]
        public void HandshakeTest()
        {
            string strExpected = "HI";
            AsyncProcess clsAsyncProcess = new AsyncProcess();
            string strResult = clsAsyncProcess.StartClient("HELO", "");
            Assert.AreEqual(strExpected, strResult);
        }
        [TestMethod]
        public void HandshakeCountTest()
        {            
            AsyncProcess clsAsyncProcess = new AsyncProcess();
            string strResult = clsAsyncProcess.StartClient("COUNT", "");
            Assert.IsNotNull(strResult);
        }
        [TestMethod]
        public void ConnectionsCountTest()
        {
            AsyncProcess clsAsyncProcess = new AsyncProcess();
            string strResult = clsAsyncProcess.StartClient("CONNECTIONS", "");
            Assert.IsNotNull(strResult);
        }
        [TestMethod]
        public void PrimeTest()
        {
            AsyncProcess clsAsyncProcess = new AsyncProcess();
            string strResult = clsAsyncProcess.StartClient("PRIME", "");
            Assert.IsNotNull(strResult);
            if (!string.IsNullOrEmpty(strResult))
            {
                Assert.IsTrue(CheckIsPrimeNumber(Convert.ToInt32(strResult)));
            }
        }
         [TestMethod]
        public void TerminateTest()
        {
            string strExpected = "BYE";
            AsyncProcess clsAsyncProcess = new AsyncProcess();
            string strResult = clsAsyncProcess.StartClient("TERMINATE", "");
            Assert.AreEqual(strExpected, strResult);
        }
        private static bool CheckIsPrimeNumber(int intNumber)
        {

            if (intNumber == 1) return false;
            if (intNumber == 2) return true;

            if (intNumber % 2 == 0) return false; // Even number     

            for (int i = 2; i < intNumber; i++)
            { // Advance from two to include correct calculation for '4'
                if (intNumber % i == 0) return false;
            }

            return true;

        }
    }
}
