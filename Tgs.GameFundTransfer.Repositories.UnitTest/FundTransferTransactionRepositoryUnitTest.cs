using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;
using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.Repositories.Entities;
using Tgs.SerialNumberGenerator;

namespace Tgs.GameFundTransfer.Repositories.UnitTest;

[TestClass]
public class FundTransferTransactionRepositoryUnitTest
{
    [TestMethod]
    public async Task Add_FundIn()
    {
        var options = new DbContextOptionsBuilder<TgsContext>()
           .UseInMemoryDatabase(databaseName: "tgs-fund-in")
           .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
           .Options;
        var context = new TgsContext(options);
        _ = context.Database.EnsureDeleted();

        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

        var repository = new FundTransferTransactionRepository(
            context,
            fakeSerialNumberGenerator);

        var gameProviderFundTransferTransactionId = Guid.NewGuid();
        var gameProviderId = Guid.NewGuid();
        var gameProviderCode = "GP";
        var playerCode = "test";
        var walletId = Guid.NewGuid();
        var amount = 10.0m;

        _ = fakeSerialNumberGenerator
            .GenerateAsync(
            cancellationToken: Arg.Any<CancellationToken>())
            .Returns(gameProviderFundTransferTransactionId);

        var actual = await repository
            .AddFundInAsync(
            gameProviderId: gameProviderId,
            gameProviderCode: gameProviderCode,
            playerCode: playerCode,
            walletId: walletId,
            amount: amount)
            .ConfigureAwait(false);

        Assert.AreEqual(gameProviderFundTransferTransactionId, actual);

        var data = await context.GameProviderFundTransferTransactions
            .AsQueryable()
            .SingleAsync(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
            .ConfigureAwait(false);

        Assert.AreEqual(gameProviderCode, data.FromWallet);
        Assert.AreEqual(WalletDef.MainWallet, data.ToWallet);
        Assert.AreEqual((short)FundSource.FundIn, data.FundSource);
        Assert.AreEqual((short)FundTransferTransactionStatus.GameProviderFundFailed, data.Status);
        Assert.AreEqual(amount, data.Amount);
    }

    [TestMethod]
    public async Task Add_FundOut()
    {
        var options = new DbContextOptionsBuilder<TgsContext>()
           .UseInMemoryDatabase(databaseName: "tgs-fund-out")
           .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
           .Options;
        var context = new TgsContext(options);
        _ = context.Database.EnsureDeleted();

        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

        var repository = new FundTransferTransactionRepository(
            context,
            fakeSerialNumberGenerator);

        var gameProviderFundTransferTransactionId = Guid.NewGuid();
        var gameProviderId = Guid.NewGuid();
        var gameProviderCode = "GP";
        var playerCode = "test";
        var walletId = Guid.NewGuid();
        var amount = 10.0m;

        _ = fakeSerialNumberGenerator
            .GenerateAsync(
             cancellationToken: Arg.Any<CancellationToken>())
            .Returns(gameProviderFundTransferTransactionId);

        var actual = await repository
            .AddFundOutAsync(
            gameProviderId: gameProviderId,
            gameProviderCode: gameProviderCode,
            playerCode: playerCode,
            walletId: walletId,
            amount: amount)
            .ConfigureAwait(false);

        Assert.AreEqual(actual, gameProviderFundTransferTransactionId);

        var data = await context.GameProviderFundTransferTransactions
            .AsQueryable()
            .SingleAsync(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
            .ConfigureAwait(false);

        Assert.AreEqual(WalletDef.MainWallet, data.FromWallet);
        Assert.AreEqual(gameProviderCode, data.ToWallet);
        Assert.AreEqual((short)FundSource.FundOut, data.FundSource);
        Assert.AreEqual((short)FundTransferTransactionStatus.MainWalletFundFailed, data.Status);
        Assert.AreEqual(-amount, data.Amount);
    }

    [TestMethod]
    public async Task Update_By_GameProvider_FundFailed()
    {
        var options = new DbContextOptionsBuilder<TgsContext>()
            .UseInMemoryDatabase(databaseName: "tgs-update-by-gameprovider-fundfailed")
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new TgsContext(options);
        _ = context.Database.EnsureDeleted();

        var gameProviderFundTransferTransactionId = Guid.NewGuid();
        var gameProviderId = Guid.NewGuid();
        var gameProviderCode = "GP";
        var playerCode = "test";
        var walletId = Guid.NewGuid();
        var amount = 10.0m;

        var fundTransferTransaction = new GameProviderFundTransferTransaction()
        {
            GameProviderFundTransferTransactionId = gameProviderFundTransferTransactionId,
            GameProviderId = gameProviderId,
            WalletId = walletId,
            PlayerCode = playerCode,
            FundSource = (short)FundSource.FundOut,
            FromWallet = WalletDef.MainWallet,
            ToWallet = gameProviderCode,
            Amount = -amount,
            Status = (short)FundTransferTransactionStatus.MainWalletFundFailed,
            CreatedOn = DateTime.Now,
            UpdatedOn = DateTime.Now
        };

        _ = await context.GameProviderFundTransferTransactions.AddAsync(fundTransferTransaction).ConfigureAwait(false);

        _ = await context.SaveChangesAsync().ConfigureAwait(false);

        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

        var repository = new FundTransferTransactionRepository(
            context,
            fakeSerialNumberGenerator);

        await repository
            .UpdateByGameProviderFundFailedAsync(
            gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId)
            .ConfigureAwait(false);

        var data = await context.GameProviderFundTransferTransactions
            .AsQueryable()
            .SingleAsync(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
            .ConfigureAwait(false);

        Assert.AreEqual((short)FundTransferTransactionStatus.GameProviderFundFailed, data.Status);
    }

    [TestMethod]
    public async Task Update_By_MainWallet_FundFailed()
    {
        var options = new DbContextOptionsBuilder<TgsContext>()
            .UseInMemoryDatabase(databaseName: "tgs-update-by-mainwallet-fundfailed")
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new TgsContext(options);
        _ = context.Database.EnsureDeleted();

        var gameProviderFundTransferTransactionId = Guid.NewGuid();
        var gameProviderId = Guid.NewGuid();
        var gameProviderCode = "GP";
        var playerCode = "test";
        var walletId = Guid.NewGuid();
        var amount = 10.0m;

        var fundTransferTransaction = new GameProviderFundTransferTransaction()
        {
            GameProviderFundTransferTransactionId = gameProviderFundTransferTransactionId,
            GameProviderId = gameProviderId,
            WalletId = walletId,
            PlayerCode = playerCode,
            FundSource = (short)FundSource.FundIn,
            FromWallet = gameProviderCode,
            ToWallet = WalletDef.MainWallet,
            Amount = amount,
            Status = (short)FundTransferTransactionStatus.GameProviderFundFailed,
            CreatedOn = DateTime.Now,
            UpdatedOn = DateTime.Now
        };

        _ = await context.GameProviderFundTransferTransactions.AddAsync(fundTransferTransaction).ConfigureAwait(false);

        _ = await context.SaveChangesAsync().ConfigureAwait(false);

        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

        var gameProviderTransactionId = Guid.NewGuid().ToString("N");

        var repository = new FundTransferTransactionRepository(
            context,
            fakeSerialNumberGenerator);

        await repository
            .UpdateByMainWalletFundFailedAsync(
            gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
            gameProviderTransactionId: gameProviderTransactionId)
            .ConfigureAwait(false);

        var data = await context.GameProviderFundTransferTransactions
            .AsQueryable()
            .SingleAsync(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
            .ConfigureAwait(false);

        Assert.AreEqual((short)FundTransferTransactionStatus.MainWalletFundFailed, data.Status);
        Assert.AreEqual(gameProviderTransactionId, data.GameProviderTransactionId);
    }

    [TestMethod]
    public async Task Update_By_MainWallet_FundRefund_Failed()
    {
        var options = new DbContextOptionsBuilder<TgsContext>()
            .UseInMemoryDatabase(databaseName: "tgs-update-by-mainwallet-fundrefund-failed")
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new TgsContext(options);
        _ = context.Database.EnsureDeleted();

        var gameProviderFundTransferTransactionId = Guid.NewGuid();
        var gameProviderId = Guid.NewGuid();
        var gameProviderCode = "GP";
        var playerCode = "test";
        var walletId = Guid.NewGuid();
        var amount = 10.0m;

        var fundTransferTransaction = new GameProviderFundTransferTransaction()
        {
            GameProviderFundTransferTransactionId = gameProviderFundTransferTransactionId,
            GameProviderId = gameProviderId,
            WalletId = walletId,
            PlayerCode = playerCode,
            FundSource = (short)FundSource.FundOut,
            FromWallet = WalletDef.MainWallet,
            ToWallet = gameProviderCode,
            Amount = -amount,
            Status = (short)FundTransferTransactionStatus.MainWalletFundFailed,
            CreatedOn = DateTime.Now,
            UpdatedOn = DateTime.Now
        };

        _ = await context.GameProviderFundTransferTransactions.AddAsync(fundTransferTransaction).ConfigureAwait(false);

        _ = await context.SaveChangesAsync().ConfigureAwait(false);

        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

        var repository = new FundTransferTransactionRepository(
            context,
            fakeSerialNumberGenerator);

        await repository
            .UpdateByMainWalletFundRefundFailedAsync(
            gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
            description: Arg.Any<string>())
            .ConfigureAwait(false);

        var data = await context.GameProviderFundTransferTransactions
            .AsQueryable()
            .SingleAsync(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
            .ConfigureAwait(false);

        Assert.AreEqual((short)FundTransferTransactionStatus.MainWalletFundRefundFailed, data.Status);
    }

    [TestMethod]
    public async Task Update_By_MainWallet_FundRefund_Success()
    {
        var options = new DbContextOptionsBuilder<TgsContext>()
            .UseInMemoryDatabase(databaseName: "tgs-update-by-mainwallet-fundrefund-success")
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new TgsContext(options);
        _ = context.Database.EnsureDeleted();

        var gameProviderFundTransferTransactionId = Guid.NewGuid();
        var gameProviderId = Guid.NewGuid();
        var gameProviderCode = "GP";
        var playerCode = "test";
        var walletId = Guid.NewGuid();
        var amount = 10.0m;

        var fundTransferTransaction = new GameProviderFundTransferTransaction()
        {
            GameProviderFundTransferTransactionId = gameProviderFundTransferTransactionId,
            GameProviderId = gameProviderId,
            WalletId = walletId,
            PlayerCode = playerCode,
            FundSource = (short)FundSource.FundOut,
            FromWallet = WalletDef.MainWallet,
            ToWallet = gameProviderCode,
            Amount = -amount,
            Status = (short)FundTransferTransactionStatus.MainWalletFundFailed,
            CreatedOn = DateTime.Now,
            UpdatedOn = DateTime.Now
        };

        _ = await context.GameProviderFundTransferTransactions.AddAsync(fundTransferTransaction).ConfigureAwait(false);

        _ = await context.SaveChangesAsync().ConfigureAwait(false);

        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

        var repository = new FundTransferTransactionRepository(
            context,
            fakeSerialNumberGenerator);

        var transactionId = Guid.NewGuid();
        var balance = 0m;

        await repository
            .UpdateByMainWalletFundRefundSuccessAsync(
            gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
            transactionId: transactionId,
            balance: balance,
            description: Arg.Any<string>())
            .ConfigureAwait(false);

        var data = await context.GameProviderFundTransferTransactions
            .AsQueryable()
            .SingleAsync(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
            .ConfigureAwait(false);

        Assert.AreEqual((short)FundTransferTransactionStatus.MainWalletFundRefundSuccess, data.Status);
        Assert.AreEqual(transactionId, data.TransactionId);
        Assert.AreEqual(balance, data.Balance);
    }

    [TestMethod]
    public async Task Update_By_Success()
    {
        var options = new DbContextOptionsBuilder<TgsContext>()
            .UseInMemoryDatabase(databaseName: "tgs-update-by-success")
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new TgsContext(options);
        _ = context.Database.EnsureDeleted();

        var gameProviderFundTransferTransactionId = Guid.NewGuid();
        var gameProviderId = Guid.NewGuid();
        var gameProviderCode = "GP";
        var playerCode = "test";
        var walletId = Guid.NewGuid();
        var amount = 10.0m;

        var fundTransferTransaction = new GameProviderFundTransferTransaction()
        {
            GameProviderFundTransferTransactionId = gameProviderFundTransferTransactionId,
            GameProviderId = gameProviderId,
            WalletId = walletId,
            PlayerCode = playerCode,
            FundSource = (short)FundSource.FundOut,
            FromWallet = WalletDef.MainWallet,
            ToWallet = gameProviderCode,
            Amount = -amount,
            Status = (short)FundTransferTransactionStatus.MainWalletFundFailed,
            CreatedOn = DateTime.Now,
            UpdatedOn = DateTime.Now
        };

        _ = await context.GameProviderFundTransferTransactions.AddAsync(fundTransferTransaction).ConfigureAwait(false);

        _ = await context.SaveChangesAsync().ConfigureAwait(false);

        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

        var repository = new FundTransferTransactionRepository(
            context,
            fakeSerialNumberGenerator);

        var transactionId = Guid.NewGuid();
        var gameProviderTransactionId = Guid.NewGuid().ToString("N");
        var balance = 0m;

        await repository
            .UpdateBySuccessAsync(
            gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
            transactionId: transactionId,
            gameProviderTransactionId: gameProviderTransactionId,
            balance: balance)
            .ConfigureAwait(false);

        var data = await context.GameProviderFundTransferTransactions
            .AsQueryable()
            .SingleAsync(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
            .ConfigureAwait(false);

        Assert.AreEqual((short)FundTransferTransactionStatus.Success, data.Status);
        Assert.AreEqual(transactionId, data.TransactionId);
        Assert.AreEqual(gameProviderTransactionId, data.GameProviderTransactionId);
        Assert.AreEqual(balance, data.Balance);
    }
}
