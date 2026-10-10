using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace BoidsWeb.Services;

public sealed class BoidDto
{
    public int Id { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Speed { get; set; }
}

public sealed class CreateBoidRequest
{
    public double X { get; set; }
    public double Y { get; set; }

    [Range(0, double.MaxValue)]
    public double Speed { get; set; }
}

public sealed class BoidsApiClient(HttpClient http)
{
    public async Task<IReadOnlyList<BoidDto>> GetBoidsAsync(
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response =
            await http.GetAsync("api/boids", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"The boids API returned {(int)response.StatusCode} " +
                $"({response.StatusCode}).",
                null,
                response.StatusCode);
        }

        List<BoidDto>? boids =
            await response.Content.ReadFromJsonAsync<List<BoidDto>>(
                cancellationToken: cancellationToken);

        return boids ?? [];
    }

    public async Task<BoidDto?> CreateBoidAsync(
        CreateBoidRequest request,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response =
            await http.PostAsJsonAsync(
                "api/boids",
                request,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"The boids API returned {(int)response.StatusCode} " +
                $"({response.StatusCode}).",
                null,
                response.StatusCode);
        }

        // Successful responses such as 204 may have no body.
        if (response.StatusCode == HttpStatusCode.NoContent ||
            response.Content.Headers.ContentLength == 0)
        {
            return null;
        }

        string content =
            await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        return JsonSerializer.Deserialize<BoidDto>(
            content,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}