using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Meadow.JsonRpc.Types;
using Meadow.JsonRpc.Client;
using Meadow.UnitTestTemplate;
using Meadow.Core.Utils;
using Meadow.Core.EthTypes;
using Meadow.Contract;
using Meadow.CoverageReport.Debugging;
using Microsoft.Win32.SafeHandles;


namespace Tune
{
    [TestClass]
    public class SafeMathTests : ContractTest
    {
        protected SafeMathTest contract;
        protected override async Task BeforeEach()
        {
            contract = await SafeMathTest.New(RpcClient);
        }

        [TestMethod]
        public async Task SkipOperationMult0()
        {
            Assert.AreEqual(0, await contract.mulZero().Call());
        }

        [TestMethod]
        public async Task RevertMultiplyOverflow()
        {
            await contract.mulOverflow().ExpectRevertCall();
        }

        [TestMethod]
        public async Task AllowRegularMultiply()
        {
            Assert.AreEqual(25, await contract.mul().Call());
        }

        [TestMethod]
        public async Task RevertDivideBy0()
        {
            await contract.divZero().ExpectRevertCall();
        }

        [TestMethod]
        public async Task AllowRegularDivision()
        {
            Assert.AreEqual(5, await contract.div().Call());
        }

        [TestMethod]
        public async Task RevertSubtractionOverflow()
        {
            await contract.subOverflow().ExpectRevertCall();
        }

        [TestMethod]
        public async Task AllowRegularSubtraction()
        {
            Assert.AreEqual(5, await contract.sub().Call());
        }

        [TestMethod]
        public async Task RevertAdditionOverflow()
        {
            await contract.addOverflow().ExpectRevertCall();
        }

        [TestMethod]
        public async Task AllowRegularAddition()
        {
            Assert.AreEqual(10, await contract.add().Call());
        }

        [TestMethod]
        public async Task mod_dividendisNotZero_shouldReturnCorrectValue()
        {
            await contract.modPass();
        }

        [TestMethod]
        public async Task mod_dividendIsZero_shouldRevert()
        {
            await contract.modZero().ExpectRevertTransaction();
        }
    }

    [TestClass]
    public class SafeMath64Tests : ContractTest
    {
        protected SafeMathTest64 contract;
        protected override async Task BeforeEach()
        {
            contract = await SafeMathTest64.New(RpcClient);
        }

        [TestMethod]
        public async Task RevertMultiplyOverflow()
        {
            await contract.mul64Overflow().ExpectRevertCall();
        }

        [TestMethod]
        public async Task RevertDivideBy0()
        {
            await contract.div64Zero().ExpectRevertCall();
        }

        [TestMethod]
        public async Task RevertSubtractionOverflow()
        {
            await contract.sub64Overflow().ExpectRevertCall();
        }

        [TestMethod]
        public async Task RevertAdditionOverflow()
        {
            await contract.add64Overflow().ExpectRevertCall();
        }

        [TestMethod]
        public async Task mod_dividendIsZero_shouldRevert()
        {
            await contract.mod64Zero().ExpectRevertTransaction();
        }
    }
}
