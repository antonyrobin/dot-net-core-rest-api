using System.Net;
using System.Net.Http.Json;
using dot_net_core_rest_api.Dtos;

namespace dot_net_core_rest_api.Tests;

public class CategoriesIntegrationTests : IClassFixture<IntegrationTestFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly IntegrationTestFactory _factory;
    private const string BaseUrl = "/api/categories";

    public CategoriesIntegrationTests(IntegrationTestFactory factory)
    {
        _factory = factory;
        _client = factory.HttpClient;
    }

    public Task InitializeAsync() => ClearCategoriesAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task ClearCategoriesAsync()
    {
        // Clean up via API: get all then delete each
        var all = await _client.GetFromJsonAsync<List<CategoryDto>>(BaseUrl);
        if (all is not null)
        {
            foreach (var c in all)
                await _client.DeleteAsync($"{BaseUrl}/{c.Id}");
        }
    }

    // ───── GET /api/categories ─────

    [Fact]
    public async Task GetAll_EmptyDatabase_ReturnsOkWithEmptyList()
    {
        var response = await _client.GetAsync(BaseUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();
        Assert.NotNull(categories);
        Assert.Empty(categories);
    }

    [Fact]
    public async Task GetAll_WithData_ReturnsAllCategories()
    {
        await _client.PostAsJsonAsync(BaseUrl, new CreateCategoryRequest("C1", "Category 1"));
        await _client.PostAsJsonAsync(BaseUrl, new CreateCategoryRequest("C2", "Category 2"));

        var response = await _client.GetAsync(BaseUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();
        Assert.Equal(2, categories!.Count);
    }

    // ───── GET /api/categories/{id} ─────

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var createResponse = await _client.PostAsJsonAsync(BaseUrl, new CreateCategoryRequest("GB1", "GetById Test"));
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var response = await _client.GetAsync($"{BaseUrl}/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var category = await response.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.Equal("GB1", category!.Code);
        Assert.Equal("GetById Test", category.Name);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"{BaseUrl}/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ───── POST /api/categories ─────

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreated()
    {
        var request = new CreateCategoryRequest("NEW1", "New Category");

        var response = await _client.PostAsJsonAsync(BaseUrl, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.NotNull(created);
        Assert.True(created!.Id > 0);
        Assert.Equal("NEW1", created.Code);
        Assert.Equal("New Category", created.Name);
        Assert.NotNull(response.Headers.Location);
    }

    // ───── PUT /api/categories/{id} ─────

    [Fact]
    public async Task Update_ExistingId_ReturnsOkWithUpdatedData()
    {
        var createResponse = await _client.PostAsJsonAsync(BaseUrl, new CreateCategoryRequest("UPD1", "Original"));
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var updateRequest = new UpdateCategoryRequest("UPD2", "Updated Name");
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created!.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.Equal("UPD2", updated!.Code);
        Assert.Equal("Updated Name", updated.Name);
    }

    [Fact]
    public async Task Update_PartialUpdate_OnlyUpdatesProvidedFields()
    {
        var createResponse = await _client.PostAsJsonAsync(BaseUrl, new CreateCategoryRequest("PART1", "Keep This Name"));
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var updateRequest = new UpdateCategoryRequest("PART2", null);
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created!.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.Equal("PART2", updated!.Code);
        Assert.Equal("Keep This Name", updated.Name);
    }

    [Fact]
    public async Task Update_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/99999", new UpdateCategoryRequest("X", "Y"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ───── DELETE /api/categories/{id} ─────

    [Fact]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        var createResponse = await _client.PostAsJsonAsync(BaseUrl, new CreateCategoryRequest("DEL1", "Delete Me"));
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var response = await _client.DeleteAsync($"{BaseUrl}/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's gone
        var getResponse = await _client.GetAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"{BaseUrl}/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ───── Full CRUD Lifecycle ─────

    [Fact]
    public async Task FullCrudLifecycle_WorksEndToEnd()
    {
        // Create
        var createResponse = await _client.PostAsJsonAsync(BaseUrl, new CreateCategoryRequest("LIFE1", "Lifecycle"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryDto>();

        // Read
        var getResponse = await _client.GetAsync($"{BaseUrl}/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // Update
        var updateResponse = await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", new UpdateCategoryRequest("LIFE2", "Updated"));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.Equal("LIFE2", updated!.Code);

        // Delete
        var deleteResponse = await _client.DeleteAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify deleted
        var verifyResponse = await _client.GetAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, verifyResponse.StatusCode);
    }
}
