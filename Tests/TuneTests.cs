using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Meadow.UnitTestTemplate;
using Meadow.JsonRpc.Types;
using Meadow.Core.EthTypes;

namespace Tune
{
    [TestClass]
    public class TuneVestingTests : ContractTest
    {
        TokenVesting vesting;
        ERC20 token;
        protected UInt64 cliffDuration;
        protected UInt64 vestingDuration;
        protected UInt64 startTime;
        protected UInt256 nowTime;
        protected Address beneficiary;

        //bytes32 constant byteText = "HelloStackOverFlow";

        private UInt256 totalSup = 1e27;
        private UInt64 secondsInMonth = 2628000;
        private UInt256 monthlyVest = 1e26;
        
        protected override async Task BeforeEach()
        {
            // Deploy contract.
            nowTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            cliffDuration = secondsInMonth * 2;
            vestingDuration = secondsInMonth * 12;
            startTime = 1564012800;
            beneficiary = Accounts[99];
            token = await ERC20.New(Accounts[0], 1e27, RpcClient);
            vesting = await TokenVesting.New(beneficiary, token.ContractAddress, (ulong)nowTime, cliffDuration, vestingDuration, true, totalSup, RpcClient);
            await token.transfer(vesting.ContractAddress, 1e27);
        }

        [TestMethod]
        public async Task constructor_BeneficiaryNotAddressZero_ExpectRevert()
        {
            await TokenVesting.New(Address.Zero, token.ContractAddress, startTime, cliffDuration, vestingDuration, true, totalSup, RpcClient).ExpectRevert();
        }

        [TestMethod]
        public async Task constructor_TokenNotAddressZero_ExpectRevert()
        {
            await TokenVesting.New(beneficiary, Address.Zero, startTime, cliffDuration, vestingDuration, true, totalSup, RpcClient).ExpectRevert();
        }

        [TestMethod]
        public async Task constructor_CliffLessThanVesting_ExpectRevert()
        {
            await TokenVesting.New(beneficiary, token.ContractAddress, startTime, 500, 200, true, totalSup, RpcClient).ExpectRevert();
        }

        [TestMethod]
        public async Task constructor_StartGreaterThanZero_ExpectRevert()
        {
            await TokenVesting.New(beneficiary, token.ContractAddress, 0, cliffDuration, vestingDuration, true, totalSup, RpcClient).ExpectRevert();
        }

        [TestMethod]
        public async Task constructor_StartAndVestingGreaterThanNow_ExpectRevert()
        {
            await TokenVesting.New(beneficiary, token.ContractAddress, 1000, cliffDuration, vestingDuration, true, totalSup, RpcClient).ExpectRevert();
        }

        [TestMethod]
        public async Task constructor_TotalReleasingTimeModSecondPerMonth_ExpectRevert()
        {
            await TokenVesting.New(beneficiary, token.ContractAddress, startTime, 2627999, 2628001, true, totalSup, RpcClient).ExpectRevert();
        }

        [TestMethod]
        public async Task beneficiary_Return_AssertEqual()
        {
            var result = await vesting.beneficiary().Call();
            Assert.AreEqual(Accounts[99], result);
        }

        [TestMethod]
        public async Task token_Return_AssertEqual()
        {
            var result = await vesting.token().Call();
            Assert.AreEqual(token.ContractAddress, result);
        }

        [TestMethod]
        public async Task cliff_Return_AssertEqual()
        {
            var result = await vesting.cliff().Call();
            var cliff = cliffDuration + nowTime;
            Assert.AreEqual(cliff, result);
        }

        [TestMethod]
        public async Task start_Return_AssertEqual()
        {
            var result = await vesting.start().Call();
            Assert.AreEqual(nowTime, result);
        }

        [TestMethod]
        public async Task vestingDuration_Return_AssertEqual()
        {
            var result = await vesting.vestingDuration().Call();
            Assert.AreEqual(vestingDuration, result);
        }

        [TestMethod]
        public async Task monthsToVest_Return_AssertEqual()
        {
            var months = await vesting.monthsToVest().Call();
            Assert.AreEqual(10, months);
        }

        [TestMethod]
        public async Task amountVested_FullYearVest_AssertEqual()
        {
            
            await RpcClient.IncreaseTime(secondsInMonth*10);
            await RpcClient.Mine();
            var result = await vesting.amountVested().Call();
            Assert.AreEqual(8*monthlyVest, result);
        }

        [TestMethod]
        public async Task amountVested_PastCliff_AssertEqual()
        {
            await RpcClient.IncreaseTime(secondsInMonth*3);
            await RpcClient.Mine();
            var result = await vesting.amountVested().Call();
            Assert.AreEqual(monthlyVest, result);
        }
        
        [TestMethod]
        public async Task revocable_Return_AssertTrue()
        {
            var result = await vesting.revocable().Call();
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public async Task revoked_NotRevoked_AssertBool()
        {
            await RpcClient.IncreaseTime(secondsInMonth*8);
            var result = await vesting.revoked().Call();
            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public async Task release_PartialTokensVest_AssertAmountReleased()
        {
            await RpcClient.IncreaseTime(secondsInMonth*5);
            var events = await vesting.release().FirstEventLog<TokenVesting.TokensReleased>();
            Assert.AreEqual(3*monthlyVest, events.amount);
            var bal = await token.balanceOf(Accounts[99]).Call();
            Assert.AreEqual(3*monthlyVest, bal);
            var released = await vesting.released().Call();
            Assert.AreEqual(3*monthlyVest, released);
        }

        [TestMethod]
        public async Task release_CliffNotStarted_ExpectRevert()
        {
            await RpcClient.IncreaseTime(secondsInMonth*1);
            await vesting.release().ExpectRevertTransaction();
        }

        [TestMethod]
        public async Task revoke_Revoked_EmitEvent()
        {
            await RpcClient.IncreaseTime(secondsInMonth*3);
            var events = await vesting.revoke().FirstOrDefaultEventLog
            <TokenVesting.TokenVestingRevoked>();
            Assert.AreEqual(1e27, events.amount);
            Assert.AreEqual(10*monthlyVest, events.amount);
        }

        [TestMethod]
        public async Task revoke_NothingVested_ExpectRevert()
        {
            await RpcClient.IncreaseTime(36792000);
            await vesting.revoke().ExpectRevertTransaction();
        }

        [TestMethod]
        public async Task revoke_AlreadyRevoked_ExpectRevert()
        {
            await RpcClient.IncreaseTime(secondsInMonth*3);
            await vesting.revoke();
            await vesting.revoke().ExpectRevertTransaction();
        }

        [TestMethod]
        public async Task revoke_CliffNotStarted_ExpectRevert()
        {
            await vesting.revoke().ExpectRevertTransaction();
        }

        [TestMethod]
        public async Task isOwner_returnOwner_AssertBool()
        {
            var result = await vesting.isOwner().Call();
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public async Task renounceOwnership_OnlyOwner_EmitEvent()
        {
            var events = await vesting.renounceOwnership().FirstEventLog<Ownable.OwnershipTransferred>();
            Assert.AreEqual(Accounts[0], events.previousOwner);
            Assert.AreEqual(Address.Zero, events.newOwner);
        }

        [TestMethod]
        public async Task transferOwnership_Owner_AssertAddress()
        {
            await vesting.transferOwnership(Accounts[50]);
            var result = await vesting.owner().Call();
            Assert.AreEqual(Accounts[50], result);
        }

        [TestMethod]
        public async Task transferOwnership_NotOwner_ExpectRevert()
        {
            await vesting.transferOwnership(Accounts[50]);
            await vesting.transferOwnership(Accounts[51]).ExpectRevertTransaction();
        }

        [TestMethod]
        public async Task transferOwnership_AddressZero_ExpectRevert()
        {
            await vesting.transferOwnership(Address.Zero).ExpectRevertTransaction();
        }

        [TestMethod]
        public async Task totalSupply_TotalTokens_AssertTotal()
        {
            var total = await token.totalSupply().Call();
            Assert.AreEqual(1e27, total);
        }

        [TestMethod]
        public async Task approve_CheckAllowance_EmitEvent()
        {
            var events = await token.approve(Accounts[2], 500).FirstEventLog<ERC20.Approval>
            (new TransactionParams{ From = Accounts[1] });
            var amount = await token.allowance(Accounts[1], Accounts[2]).Call();
            Assert.AreEqual(events.value, 500);
            Assert.AreEqual(500, amount);
        }

        [TestMethod]
        public async Task approve_AddressZero_ExpectRevert()
        {
            await token.approve(Address.Zero, 1).ExpectRevertTransaction();
        }

        [TestMethod]
        public async Task transfer_AddressZero_ExpectRevert()
        {
            await token.transfer(Address.Zero, 1).ExpectRevertTransaction();
        }

        [TestMethod]
        public async Task increaseAllowance_Increase_EmitEvent()
        {
            var events = await token.increaseAllowance(Accounts[2], 500).FirstEventLog<ERC20.Approval>();
            Assert.AreEqual(500, events.value);
        }

        [TestMethod]
        public async Task increaseAllowance_AddressZero_ExpectRevert()
        {
            await token.increaseAllowance(Address.Zero, 500).ExpectRevertTransaction();
        }

        [TestMethod]
        public async Task decreaseAllowance_Decrease_EmitEvent()
        {
            await token.increaseAllowance(Accounts[2], 500);
            var events = await token.decreaseAllowance(Accounts[2], 250).FirstEventLog<ERC20.Approval>();
            Assert.AreEqual(250, events.value);
        }

        [TestMethod]
        public async Task decreaseAllowance_AddressZero_ExpectRevert()
        {
            await token.decreaseAllowance(Address.Zero, 500).ExpectRevertTransaction();
        }
    }
    [TestClass]
    public class MultiTests : ContractTest
    {
        TokenVesting vesting, secondVest, thirdVest;
        ERC20 token, secondToken, thirdToken;
        protected UInt64 cliffDuration;
        protected UInt64 vestingDuration;
        protected UInt64 startTime;
        protected UInt256 nowTime;
        private UInt256 totalSup = 1e27;
        private UInt64 secondsInMonth = 2628000;
        private UInt256 monthlyVest = 1e26;
        
        protected override async Task BeforeEach()
        {
            // Deploy contract.
            nowTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            cliffDuration = secondsInMonth * 2;
            vestingDuration = secondsInMonth * 12;
            startTime = 1564012800;
            token = await ERC20.New(Accounts[0], 1e27, RpcClient);
            secondToken = await ERC20.New(Accounts[0], 1e27, RpcClient);
            thirdToken = await ERC20.New(Accounts[0], 1e27, RpcClient);
            vesting = await TokenVesting.New(Accounts[25], token.ContractAddress, 
             (ulong)nowTime, cliffDuration, vestingDuration, true, totalSup, RpcClient);
        }

        [TestMethod]
        public async Task release_MultipleSameVests_AssertBalances()
        {
          secondVest = await TokenVesting.New(Accounts[35], secondToken.ContractAddress,
             (ulong)nowTime, cliffDuration, vestingDuration, true, totalSup, RpcClient);
            thirdVest = await TokenVesting.New(Accounts[45], thirdToken.ContractAddress,
             (ulong)nowTime, cliffDuration, vestingDuration, true, totalSup, RpcClient);
            await token.transfer(vesting.ContractAddress, totalSup);
            await secondToken.transfer(secondVest.ContractAddress, totalSup);
            await thirdToken.transfer(thirdVest.ContractAddress, totalSup);
            //vest
            await RpcClient.IncreaseTime(secondsInMonth*12);
            await vesting.release();
            await secondVest.release();
            await thirdVest.release();
            //balances after distribution
            var bal = await token.balanceOf(Accounts[25]).Call();
            var balSecond = await secondToken.balanceOf(Accounts[35]).Call();
            var balThird = await thirdToken.balanceOf(Accounts[45]).Call();
            Assert.AreEqual(10*monthlyVest, bal);
            Assert.AreEqual(10*monthlyVest, balSecond);
            Assert.AreEqual(10*monthlyVest, balThird);
        }

        [TestMethod]
        public async Task release_differentVests_AssertBalances()
        {
          secondVest = await TokenVesting.New(Accounts[35], secondToken.ContractAddress,
             (ulong)nowTime, secondsInMonth*11, vestingDuration, true, totalSup, RpcClient);
            thirdVest = await TokenVesting.New(Accounts[45], thirdToken.ContractAddress,
             (ulong)nowTime, 0, secondsInMonth*20, true, totalSup, RpcClient);
            await token.transfer(vesting.ContractAddress, totalSup);
            await secondToken.transfer(secondVest.ContractAddress, totalSup);
            await thirdToken.transfer(thirdVest.ContractAddress, totalSup);
            //vest
            await RpcClient.IncreaseTime(secondsInMonth*6);
            await vesting.release();
            await thirdVest.release();
            //balances after distribution, third account monthlyVest will be div 2 
            var bal = await token.balanceOf(Accounts[25]).Call();
            var balSecond = await secondToken.balanceOf(Accounts[35]).Call();
            var balThird = await thirdToken.balanceOf(Accounts[45]).Call();
            Assert.AreEqual(4*monthlyVest, bal);
            Assert.AreEqual(0*monthlyVest, balSecond);
            Assert.AreEqual(3*monthlyVest, balThird);
            //balances after vesting second round
            await RpcClient.IncreaseTime(secondsInMonth*14);
            await vesting.release();
            await secondVest.release();
            await thirdVest.release();
            bal = await token.balanceOf(Accounts[25]).Call();
            Assert.AreEqual(10*monthlyVest, bal);
            balSecond = await secondToken.balanceOf(Accounts[35]).Call();
            Assert.AreEqual(10*monthlyVest, balSecond);
            balThird = await thirdToken.balanceOf(Accounts[45]).Call();
            Assert.AreEqual(10*monthlyVest, balThird);
        }

        [TestMethod]
        public async Task release_timedVests_AssertBalances()
        {
          secondVest = await TokenVesting.New(Accounts[35], secondToken.ContractAddress,
             (ulong)nowTime+secondsInMonth, cliffDuration, vestingDuration, true, totalSup, RpcClient);
            thirdVest = await TokenVesting.New(Accounts[45], thirdToken.ContractAddress,
             (ulong)nowTime+secondsInMonth*3, cliffDuration, vestingDuration, true, totalSup, RpcClient);
            await token.transfer(vesting.ContractAddress, totalSup);
            await secondToken.transfer(secondVest.ContractAddress, totalSup);
            await thirdToken.transfer(thirdVest.ContractAddress, totalSup);
            //vest
            await RpcClient.IncreaseTime(secondsInMonth*12);
            await vesting.release();
            await secondVest.release();
            await thirdVest.release();
            //balances after first 12 months
            var bal = await token.balanceOf(Accounts[25]).Call();
            var balSecond = await secondToken.balanceOf(Accounts[35]).Call();
            var balThird = await thirdToken.balanceOf(Accounts[45]).Call();
            Assert.AreEqual(10*monthlyVest, bal);
            Assert.AreEqual(9*monthlyVest, balSecond);
            Assert.AreEqual(7*monthlyVest, balThird);
            //balances after vesting second round
            await RpcClient.IncreaseTime(secondsInMonth);
            await secondVest.release();
            await thirdVest.release();
            balSecond = await secondToken.balanceOf(Accounts[35]).Call();
            Assert.AreEqual(10*monthlyVest, balSecond);
            balThird = await thirdToken.balanceOf(Accounts[45]).Call();
            Assert.AreEqual(8*monthlyVest, balThird);
            //balances after vesting third round
            await RpcClient.IncreaseTime(secondsInMonth*2);
            await thirdVest.release();
            balThird = await thirdToken.balanceOf(Accounts[45]).Call();
            Assert.AreEqual(10*monthlyVest, balThird);
        }

        [TestMethod]
        public async Task safeTransfer_FailedTransfer_ExpectRevert()
        {
            secondVest = await TokenVesting.New(Accounts[78], token.ContractAddress, startTime, cliffDuration, vestingDuration, true, totalSup, RpcClient);
            await RpcClient.IncreaseTime(secondsInMonth*24);
            await secondVest.release().ExpectRevertTransaction();
        }

        [TestMethod]
        public async Task revocable_Return_AssertFalse()
        {
            secondVest = await TokenVesting.New(Accounts[78], token.ContractAddress, startTime, cliffDuration, vestingDuration, false, totalSup, RpcClient);
            var result = await secondVest.revocable().Call();
            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public async Task release_AmountSendLessZero_ExpectRevert()
        {
            secondVest = await TokenVesting.New(Accounts[78], token.ContractAddress, (ulong)nowTime, cliffDuration, vestingDuration, true, 0, RpcClient);
            await RpcClient.IncreaseTime(secondsInMonth*7);
            await secondVest.release().ExpectRevertTransaction();
        }

        [TestMethod]
        public async Task revoke_NotRevocable_ExpectRevert()
        {
            secondVest = await TokenVesting.New(Accounts[78], token.ContractAddress, startTime, cliffDuration, vestingDuration, false, totalSup, RpcClient);
            await secondVest.revoke().ExpectRevertTransaction();
        }
        
        [TestMethod]
        public async Task transferFrom_Approved_AssertBalance()
        {
            await token.approve(Accounts[16], 5000);
            await token.transferFrom(Accounts[0], Accounts[16], 5000).SendTransaction(new TransactionParams {From = Accounts[16]});
            var bal = await token.balanceOf(Accounts[16]).Call();
            Assert.AreEqual(5000, bal);
        }
    }
}
