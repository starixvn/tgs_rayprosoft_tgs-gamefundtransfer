using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.GameFundTransfer.Client;

internal class GameFundTransferClient(HttpClient httpClient) : IGameFundTransferClient
{
	private static readonly JsonSerializerOptions? _SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters =
		{
			new JsonStringEnumConverter()
		}
	};

	public async ValueTask<Common.ViewModels.FundTransferBalanceResultViewModel> FundTransferBalanceAsync(string playerCode, string gameProviderCode, CancellationToken cancellationToken = default)
	{
		var response = await httpClient.GetAsync(
			requestUri: $"/FundTransfer/{playerCode}/{gameProviderCode}/balance",
			cancellationToken: cancellationToken).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();

		var contentJson = await response.Content.ReadAsStringAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

		return JsonSerializer.Deserialize<Common.ViewModels.FundTransferBalanceResultViewModel>(contentJson, _SerializerOptions)!;
	}

	public ValueTask<Common.ViewModels.FundTransferResultViewModel?> FundTransferInAsync(string playerCode, string gameProviderCode, decimal amount, CancellationToken cancellationToken = default)
	{
		var jsonContent = JsonContent.Create(
			inputValue: new Common.ViewModels.FundTransferViewModel
			{
				Amount = amount
			},
			options: _SerializerOptions);

		return httpClient.PostAsync($"/FundTransfer/{playerCode}/{gameProviderCode}/in", jsonContent, cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.Where(response => response.StatusCode == System.Net.HttpStatusCode.OK)
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<Common.ViewModels.FundTransferResultViewModel>(_SerializerOptions, cancellationToken)
				.ToAsyncEnumerable())
			.FirstOrDefaultAsync(cancellationToken: cancellationToken);
	}

	public ValueTask<Common.ViewModels.FundTransferResultViewModel?> FundTransferInOneClickAsync(string playerCode, string gameProviderCode, CancellationToken cancellationToken = default)
	{
		return httpClient.PostAsync($"/FundTransfer/{playerCode}/{gameProviderCode}/in-one-click", null, cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.Where(response => response.StatusCode == System.Net.HttpStatusCode.OK)
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<Common.ViewModels.FundTransferResultViewModel>(_SerializerOptions, cancellationToken)
				.ToAsyncEnumerable())
			.FirstOrDefaultAsync(cancellationToken: cancellationToken);
	}

	public ValueTask<Common.ViewModels.FundTransferResultViewModel?> FundTransferOutAsync(string playerCode, string gameProviderCode, decimal amount, CancellationToken cancellationToken = default)
	{
		var jsonContent = JsonContent.Create(
			inputValue: new Common.ViewModels.FundTransferViewModel
			{
				Amount = amount
			},
			options: _SerializerOptions);

		return httpClient.PostAsync($"/FundTransfer/{playerCode}/{gameProviderCode}/out", jsonContent, cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.Where(response => response.StatusCode == System.Net.HttpStatusCode.OK)
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<Common.ViewModels.FundTransferResultViewModel>(_SerializerOptions, cancellationToken)
				.ToAsyncEnumerable())
			.FirstOrDefaultAsync(cancellationToken: cancellationToken);
	}
}
