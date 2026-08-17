using BlazorUI.Contracts;
using System.Net.Http.Json;

namespace BlazorUI.Services.Location;

public class LocationApi(HttpClient client) : ILocationApi
{
    public async Task<LocationResponse> CreateAsync(CreateLocationRequest request, CancellationToken cancellationToken)
    {
        var result = await client.PostAsJsonAsync("api/locations", request, cancellationToken);
        result.EnsureSuccessStatusCode();

        return await result.Content.ReadFromJsonAsync<LocationResponse>(cancellationToken) ?? 
            throw new InvalidOperationException("POST api/locations failed");
    }

    public async Task<PagedResponse<LocationResponse>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var result = await client.GetFromJsonAsync<PagedResponse<LocationResponse>>(
            $"api/locations?page={page}&pageSize={pageSize}",
            cancellationToken);

        return result ?? throw new InvalidOperationException("GET api/locations returned a null body.");
    }
}

