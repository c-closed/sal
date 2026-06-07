using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sboard접속기.Services.Models;

namespace Sboard접속기.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ApiService(string baseUrl, string bearerToken)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(30) };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
    }

    public async Task<Dictionary<string, UserInfo>> GetUsersAsync()
    {
        var r = await _http.GetAsync("users");
        r.EnsureSuccessStatusCode();
        var json = await r.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(json);
        if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("users", out var users))
            return JsonSerializer.Deserialize<Dictionary<string, UserInfo>>(users.GetRawText(), JsonOpts) ?? [];
        return JsonSerializer.Deserialize<Dictionary<string, UserInfo>>(json, JsonOpts) ?? [];
    }

    public async Task CreateUserAsync(string username, string userId, string userPw)
    {
        var r = await _http.PostAsJsonAsync("users", new { username, id = userId, pw = userPw }, JsonOpts);
        r.EnsureSuccessStatusCode();
    }

    public async Task UpdateUserPwOnlyAsync(string username, string userId, string newPw)
    {
        var r = await _http.PutAsJsonAsync($"users/{username}", new { id = userId, pw = newPw }, JsonOpts);
        r.EnsureSuccessStatusCode();
    }

    public async Task DeleteUserAsync(string username)
    {
        var r = await _http.DeleteAsync($"users/{username}");
        r.EnsureSuccessStatusCode();
    }


}
