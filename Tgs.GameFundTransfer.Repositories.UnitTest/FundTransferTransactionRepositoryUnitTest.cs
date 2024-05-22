using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;
using Tgs.GameFundTransfer.Common.Enums;
using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.Entities;
using Tgs.SerialNumberGenerator;

namespace Tgs.GameFundTransfer.Repositories.UnitTest;

public class FundTransferTransactionRepositoryUnitTest
{
	[Fact]
	public async Task Add_Processing_Fund_Out()
	{
		var options = new DbContextOptionsBuilder<TgsContext>()
		   .UseInMemoryDatabase(databaseName: "game_provider_fund_transfer_transactions.fund_out")
		   .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
		   .Options;
		var context = new TgsContext(options);
		_ = context.Database.EnsureDeleted();

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		var gameProviderId = Guid.NewGuid();
		var walletId = Guid.NewGuid();
		var gameProviderTransactionId = "72412202405200607050004ucmqplxb029";
		var playerCode = "test";
		var fundSource = FundSource.FundOut;
		var fromWallet = WalletDef.MainWallet;
		var toWallet = GameProviders.Domains.GameProviderType.Kaiyuan;
		var amount = 10.0m;

		var sut = new FundTransferTransactionRepository(context);

		await sut.AddProcessingAsync(
			gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
			gameProviderId: gameProviderId,
			walletId: walletId,
			gameProviderTransactionId: gameProviderTransactionId,
			playerCode: playerCode,
			fundSource: fundSource,
			fromWallet: fromWallet,
			toWallet: toWallet,
			amount: amount);

		var data = await context.GameProviderFundTransferTransactions
			.SingleAsync(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId);

		Assert.Equal(gameProviderFundTransferTransactionId, data.GameProviderFundTransferTransactionId);
		Assert.Equal(gameProviderId, data.GameProviderId);
		Assert.Null(data.TransactionId);
		Assert.Equal(gameProviderTransactionId, data.GameProviderTransactionId);
		Assert.Equal(playerCode, data.PlayerCode);
		Assert.Equal(walletId, data.WalletId);
		Assert.Equal((short)FundStatus.Processing, data.Status);
		Assert.Equal((short)FundSource.FundOut, data.FundSource);
		Assert.Equal(WalletDef.MainWallet, data.FromWallet);
		Assert.Equal(GameProviders.Domains.GameProviderType.Kaiyuan, data.ToWallet);
		Assert.Equal(amount, data.Amount);
	}

	[Fact]
	public async Task Add_Processing_Fund_In()
	{
		var options = new DbContextOptionsBuilder<TgsContext>()
		   .UseInMemoryDatabase(databaseName: "game_provider_fund_transfer_transactions.fund_in")
		   .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
		   .Options;
		var context = new TgsContext(options);
		_ = context.Database.EnsureDeleted();

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		var gameProviderId = Guid.NewGuid();
		var walletId = Guid.NewGuid();
		var gameProviderTransactionId = "72412202405200607050004ucmqplxb029";
		var playerCode = "test";
		var fundSource = FundSource.FundIn;
		var fromWallet = GameProviders.Domains.GameProviderType.Kaiyuan;
		var toWallet = WalletDef.MainWallet;
		var amount = 10.0m;

		var sut = new FundTransferTransactionRepository(context);

		await sut.AddProcessingAsync(
			gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
			gameProviderId: gameProviderId,
			walletId: walletId,
			gameProviderTransactionId: gameProviderTransactionId,
			playerCode: playerCode,
			fundSource: fundSource,
			fromWallet: fromWallet,
			toWallet: toWallet,
			amount: amount);

		var data = await context.GameProviderFundTransferTransactions
			.SingleAsync(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId);

		Assert.Equal(gameProviderFundTransferTransactionId, data.GameProviderFundTransferTransactionId);
		Assert.Equal(gameProviderId, data.GameProviderId);
		Assert.Null(data.TransactionId);
		Assert.Equal(gameProviderTransactionId, data.GameProviderTransactionId);
		Assert.Equal(playerCode, data.PlayerCode);
		Assert.Equal(walletId, data.WalletId);
		Assert.Equal((short)FundStatus.Processing, data.Status);
		Assert.Equal((short)FundSource.FundIn, data.FundSource);
		Assert.Equal(WalletDef.MainWallet, data.ToWallet);
		Assert.Equal(GameProviders.Domains.GameProviderType.Kaiyuan, data.FromWallet);
		Assert.Equal(amount, data.Amount);
	}

	[Fact]
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
			Status = (short)FundStatus.MainWalletFundFailed,
			CreatedOn = DateTime.Now,
			UpdatedOn = DateTime.Now
		};

		_ = await context.GameProviderFundTransferTransactions.AddAsync(fundTransferTransaction);
		_ = await context.SaveChangesAsync();

		var sut = new FundTransferTransactionRepository(context);

		await sut.UpdateByGameProviderFundFailedAsync(
			gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId);

		var data = await context.GameProviderFundTransferTransactions
			.SingleAsync(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId);

		Assert.Equal((short)FundStatus.GameProviderFundFailed, data.Status);
	}

	[Fact]
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
		var gameProviderTransactionId = Guid.NewGuid().ToString("N");

		var fundTransferTransaction = new GameProviderFundTransferTransaction()
		{
			GameProviderFundTransferTransactionId = gameProviderFundTransferTransactionId,
			GameProviderTransactionId = gameProviderTransactionId,
			GameProviderId = gameProviderId,
			WalletId = walletId,
			PlayerCode = playerCode,
			FundSource = (short)FundSource.FundIn,
			FromWallet = gameProviderCode,
			ToWallet = WalletDef.MainWallet,
			Amount = amount,
			Status = (short)FundStatus.GameProviderFundFailed,
			CreatedOn = DateTime.Now,
			UpdatedOn = DateTime.Now
		};

		_ = await context.GameProviderFundTransferTransactions.AddAsync(fundTransferTransaction);

		_ = await context.SaveChangesAsync();

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

		var sut = new FundTransferTransactionRepository(context);

		await sut.UpdateByMainWalletFundFailedAsync(
			gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId);

		var data = await context.GameProviderFundTransferTransactions
			.SingleAsync(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId);

		Assert.Equal((short)FundStatus.MainWalletFundFailed, data.Status);
	}

	[Fact]
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
			Status = (short)FundStatus.MainWalletFundFailed,
			CreatedOn = DateTime.Now,
			UpdatedOn = DateTime.Now
		};

		_ = await context.GameProviderFundTransferTransactions.AddAsync(fundTransferTransaction);

		_ = await context.SaveChangesAsync();

		var sut = new FundTransferTransactionRepository(context);

		await sut.UpdateByMainWalletFundRefundFailedAsync(
			gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
			description: Arg.Any<string>());

		var data = await context.GameProviderFundTransferTransactions
			.SingleAsync(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId);

		Assert.Equal((short)FundStatus.MainWalletFundRefundFailed, data.Status);
	}

	[Fact]
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
			Status = (short)FundStatus.MainWalletFundFailed,
			CreatedOn = DateTime.Now,
			UpdatedOn = DateTime.Now
		};

		_ = await context.GameProviderFundTransferTransactions.AddAsync(fundTransferTransaction);

		_ = await context.SaveChangesAsync();

		var sut = new FundTransferTransactionRepository(context);

		var transactionId = Guid.NewGuid();
		var balance = 0m;

		await sut.UpdateByMainWalletFundRefundSuccessAsync(
			gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
			transactionId: transactionId,
			balance: balance,
			description: Arg.Any<string>());

		var data = await context.GameProviderFundTransferTransactions
			.SingleAsync(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId);

		Assert.Equal((short)FundStatus.MainWalletFundRefundSuccess, data.Status);
		Assert.Equal(transactionId, data.TransactionId);
		Assert.Equal(balance, data.Balance);
	}

	[Fact]
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
		var gameProviderTransactionId = Guid.NewGuid().ToString("N");
		var playerCode = "test";
		var walletId = Guid.NewGuid();
		var amount = 10.0m;

		var fundTransferTransaction = new GameProviderFundTransferTransaction()
		{
			GameProviderFundTransferTransactionId = gameProviderFundTransferTransactionId,
			GameProviderId = gameProviderId,
			GameProviderTransactionId = gameProviderTransactionId,
			WalletId = walletId,
			PlayerCode = playerCode,
			FundSource = (short)FundSource.FundOut,
			FromWallet = WalletDef.MainWallet,
			ToWallet = gameProviderCode,
			Amount = -amount,
			Status = (short)FundStatus.MainWalletFundFailed,
			CreatedOn = DateTime.Now,
			UpdatedOn = DateTime.Now
		};

		_ = await context.GameProviderFundTransferTransactions.AddAsync(fundTransferTransaction);

		_ = await context.SaveChangesAsync();

		var sut = new FundTransferTransactionRepository(context);

		var transactionId = Guid.NewGuid();

		var balance = 0m;

		await sut.UpdateBySuccessAsync(
			gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
			transactionId: transactionId,
			balance: balance);

		var data = await context.GameProviderFundTransferTransactions
			.SingleAsync(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId);

		Assert.Equal((short)FundStatus.Success, data.Status);
		Assert.Equal(transactionId, data.TransactionId);
		Assert.Equal(gameProviderTransactionId, data.GameProviderTransactionId);
		Assert.Equal(balance, data.Balance);
	}
}
