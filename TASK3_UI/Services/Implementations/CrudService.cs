using Microsoft.Net.Http.Headers;
using ContentDispositionHeaderValue = System.Net.Http.Headers.ContentDispositionHeaderValue;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;
using System.Text;
using System.Text.Json;
using TASK3_UI.Resources;
using TASK3_UI.Services.Interfaces;

namespace TASK3_UI.Services.Implementations {
  public class CrudService : ICrudService {
    private readonly HttpClient _client;
    private readonly IHttpContextAccessor _accessor;

    public CrudService(IHttpContextAccessor contextAccessor) {
      _client = new HttpClient();
      _accessor = contextAccessor;
    }

    private void AddAuthorizationHeader() {
      var token = _accessor.HttpContext.Request.Cookies["token"];
      if (!string.IsNullOrEmpty(token)) {
        _client.DefaultRequestHeaders.Remove(HeaderNames.Authorization);
        _client.DefaultRequestHeaders.Add(HeaderNames.Authorization, token);
      }
    }

    public async Task CreateAsync<TRequest>(TRequest data, string url) {
      AddAuthorizationHeader();
      var content = CreateMultipartContent(data);
      var response = await _client.PostAsync(url, content);
      if (!response.IsSuccessStatusCode) {
        throw new HttpResponseException(response);
      }
    }

    public async Task DeleteAsync(string url) {
      AddAuthorizationHeader();
      var response = await _client.DeleteAsync(url);
      if (!response.IsSuccessStatusCode) {
        throw new HttpResponseException(response);
      }
    }

    public async Task<TResponse> GetAsync<TResponse>(string url) {
      AddAuthorizationHeader();
      var response = await _client.GetAsync(url);
      if (!response.IsSuccessStatusCode) {
        throw new HttpResponseException(response);
      }
      var bodyStr = await response.Content.ReadAsStringAsync();
      return JsonSerializer.Deserialize<TResponse>(bodyStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<PaginatedResponse<TResponse>> GetAllPaginatedAsync<TResponse>(int page, string baseUrl, Dictionary<string, string> parameters) {
      AddAuthorizationHeader();
      var queryParams = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"));
      var url = $"{baseUrl}?pageNumber={page}&{queryParams}";

      var response = await _client.GetAsync(url);
      if (!response.IsSuccessStatusCode) {
        throw new HttpResponseException(response);
      }
      var bodyStr = await response.Content.ReadAsStringAsync();
      return JsonSerializer.Deserialize<PaginatedResponse<TResponse>>(bodyStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task UpdateAsync<TRequest>(TRequest data, string url) {
      AddAuthorizationHeader();
      var content = CreateMultipartContent(data);
      var response = await _client.PutAsync(url, content);
      if (!response.IsSuccessStatusCode) {
        throw new HttpResponseException(response);
      }
    }

    public MultipartFormDataContent CreateMultipartContent<TRequest>(TRequest request) {
      var content = new MultipartFormDataContent();
      var properties = typeof(TRequest).GetProperties();

      foreach (var property in properties) {
        var value = property.GetValue(request);
        if (value != null) {
          if (property.PropertyType == typeof(IFormFile)) {
            var file = (IFormFile)value;
            var streamContent = new StreamContent(file.OpenReadStream());
            streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") {
              Name = property.Name,
              FileName = file.FileName
            };
            content.Add(streamContent);
          }
          else if (property.PropertyType == typeof(List<IFormFile>)) {
            var files = (List<IFormFile>)value;
            foreach (var file in files) {
              var streamContent = new StreamContent(file.OpenReadStream());
              streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") {
                Name = property.Name,
                FileName = file.FileName
              };
              content.Add(streamContent);
            }
          }
          else if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?)) {
            var stringContent = new StringContent(((DateTime)value).ToString("yyyy-MM-dd"));
            stringContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") {
              Name = property.Name
            };
            stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            content.Add(stringContent);
          }
          else {
            var stringContent = new StringContent(value.ToString() ?? string.Empty);
            stringContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") {
              Name = property.Name
            };
            stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            content.Add(stringContent);
          }
        }
      }

      return content;
    }
  }
}
