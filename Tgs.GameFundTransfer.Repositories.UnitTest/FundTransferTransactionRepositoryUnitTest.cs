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
           .UseInMemoryDatabase(databaseName: "tgs")
           .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
           .Options;
        var context = new TgsContext(options);
        _ = context.Database.EnsureDeleted();

        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

        var gameProviderFundTransferTransactionId = Guid.NewGuid();
        var gameProviderId = Guid.NewGuid();
        var gameProviderCode = "GP";
        var playerCode = "test";
        var walletId = Guid.NewGuid();
        var amount = 10.0m;

        _ = fakeSerialNumberGenerator.GenerateAsync()
            .Returns(gameProviderFundTransferTransactionId);

        var repository = new FundTransferTransactionRepository(
            context,
            fakeSerialNumberGenerator);

        var actual = await repository.AddFundInAsync(
            gameProviderId: gameProviderId,
            gameProviderCode: gameProviderCode,
            playerCode: playerCode,
            walletId: walletId,
            amount: amount).ConfigureAwait(false);

        Assert.IsNotNull(actual);
        Assert.IsTrue(actual == gameProviderFundTransferTransactionId);

        var data = await context
                    .GameProviderFundTransferTransactions
                    .AsQueryable()
                    .SingleAsync(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
                    .ConfigureAwait(false);

        Assert.IsTrue(data.FromWallet == gameProviderCode);
        Assert.IsTrue(data.ToWallet == WalletDef.MainWallet);
        Assert.IsTrue(data.FundSource == (short)FundSource.FundIn);
        Assert.IsTrue(data.Status == (short)FundTransferTransactionStatus.GameProviderFundFailed);
        Assert.IsTrue(data.Amount == amount);
    }

    [TestMethod]
    public async Task Add_FundOut()
    {
        var options = new DbContextOptionsBuilder<TgsContext>()
           .UseInMemoryDatabase(databaseName: "tgs")
           .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
           .Options;
        var context = new TgsContext(options);
        _ = context.Database.EnsureDeleted();

        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

        var gameProviderFundTransferTransactionId = Guid.NewGuid();
        var gameProviderId = Guid.NewGuid();
        var gameProviderCode = "GP";
        var playerCode = "test";
        var walletId = Guid.NewGuid();
        var amount = 10.0m;

        _ = fakeSerialNumberGenerator.GenerateAsync()
            .Returns(gameProviderFundTransferTransactionId);

        var repository = new FundTransferTransactionRepository(
            context,
            fakeSerialNumberGenerator);

        var actual = await repository.AddFundOutAsync(
            gameProviderId: gameProviderId,
            gameProviderCode: gameProviderCode,
            playerCode: playerCode,
            walletId: walletId,
            amount: amount).ConfigureAwait(false);

        Assert.IsNotNull(actual);
        Assert.IsTrue(actual == gameProviderFundTransferTransactionId);

        var data = await context
                    .GameProviderFundTransferTransactions
                    .AsQueryable()
                    .SingleAsync(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
                    .ConfigureAwait(false);

        Assert.IsTrue(data.FromWallet == WalletDef.MainWallet);
        Assert.IsTrue(data.ToWallet == gameProviderCode);
        Assert.IsTrue(data.FundSource == (short)FundSource.FundOut);
        Assert.IsTrue(data.Status == (short)FundTransferTransactionStatus.MainWalletFundFailed);
        Assert.IsTrue(data.Amount == -amount);
    }

    [TestMethod]
    public async Task Update_By_GameProvider_FundFailed()
    {
        var options = new DbContextOptionsBuilder<TgsContext>()
                       .UseInMemoryDatabase(databaseName: "tgs")
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

        await repository.UpdateByGameProviderFundFailedAsync(
                gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId).ConfigureAwait(false);

        var data = await context
                    .GameProviderFundTransferTransactions
                    .AsQueryable()
                    .SingleAsync(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
                    .ConfigureAwait(false);

        Assert.IsTrue(data.Status == (short)FundTransferTransactionStatus.GameProviderFundFailed);
    }

    [TestMethod]
    public async Task Update_By_MainWallet_FundFailed()
    {
        var options = new DbContextOptionsBuilder<TgsContext>()
                       .UseInMemoryDatabase(databaseName: "tgs")
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

        var repository = new FundTransferTransactionRepository(
            context,
            fakeSerialNumberGenerator);

        var gameProviderTransactionId = Guid.NewGuid().ToString("N");

        await repository.UpdateByMainWalletFundFailedAsync(
            gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
            gameProviderTransactionId: gameProviderTransactionId).ConfigureAwait(false);

        var data = await context
                    .GameProviderFundTransferTransactions
                    .AsQueryable()
                    .SingleAsync(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
                    .ConfigureAwait(false);

        Assert.IsTrue(data.Status == (short)FundTransferTransactionStatus.MainWalletFundFailed);
        Assert.IsTrue(data.GameProviderTransactionId == gameProviderTransactionId);
    }

    [TestMethod]
    public async Task Update_By_MainWallet_FundRefund_Failed()
    {
        var options = new DbContextOptionsBuilder<TgsContext>()
                       .UseInMemoryDatabase(databaseName: "tgs")
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

        await repository.UpdateByMainWalletFundRefundFailedAsync(
                gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
                description: Arg.Any<string>()).ConfigureAwait(false);

        var data = await context
                    .GameProviderFundTransferTransactions
                    .AsQueryable()
                    .SingleAsync(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
                    .ConfigureAwait(false);

        Assert.IsTrue(data.Status == (short)FundTransferTransactionStatus.MainWalletFundRefundFailed);
    }

    [TestMethod]
    public async Task Update_By_MainWallet_FundRefund_Success_Async()
    {
        var options = new DbContextOptionsBuilder<TgsContext>()
                       .UseInMemoryDatabase(databaseName: "tgs")
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
        await repository.UpdateByMainWalletFundRefundSuccessAsync(
                gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
                transactionId: transactionId,
                balance: balance,
                description: Arg.Any<string>()).ConfigureAwait(false);

        var data = await context
                    .GameProviderFundTransferTransactions
                    .AsQueryable()
                    .SingleAsync(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
                    .ConfigureAwait(false);

        Assert.IsTrue(data.Status == (short)FundTransferTransactionStatus.MainWalletFundRefundSuccess);
        Assert.IsTrue(data.TransactionId == transactionId);
        Assert.IsTrue(data.Balance == balance);
    }

    [TestMethod]
    public async Task Update_By_Success()
    {
        var options = new DbContextOptionsBuilder<TgsContext>()
                       .UseInMemoryDatabase(databaseName: "tgs")
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
        await repository.UpdateBySuccessAsync(
                gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
                transactionId: transactionId,
                gameProviderTransactionId: gameProviderTransactionId,
                balance: balance).ConfigureAwait(false);

        var data = await context
                    .GameProviderFundTransferTransactions
                    .AsQueryable()
                    .SingleAsync(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
                    .ConfigureAwait(false);

        Assert.IsTrue(data.Status == (short)FundTransferTransactionStatus.Success);
        Assert.IsTrue(data.TransactionId == transactionId);
        Assert.IsTrue(data.GameProviderTransactionId == gameProviderTransactionId);
        Assert.IsTrue(data.Balance == balance);
    }
}
