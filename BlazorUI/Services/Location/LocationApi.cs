using BlazorUI.Contracts;
using System.Net.Http.Json;

namespace BlazorUI.Services.Location;

public class LocationApi(HttpClient client) : ILocationApi
{
    public async Task<PagedResponse<LocationResponse>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var result = await client.GetFromJsonAsync<PagedResponse<LocationResponse>>(
            $"api/locations?page={page}&pageSize={pageSize}",
            cancellationToken);

        return result ?? throw new InvalidOperationException("GET api/locations returned a null body.");
    }
}

