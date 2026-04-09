using System.Net;
using System.Net.Http.Json;
using dot_net_core_rest_api.Dtos;

namespace dot_net_core_rest_api.Tests;

public class SubCategoriesIntegrationTests : IClassFixture<IntegrationTestFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly IntegrationTestFactory _factory;
    private const string BaseUrl = "/api/subcategories";
    private const string CategoriesUrl = "/api/categories";
    private int _categoryId;

    public SubCategoriesIntegrationTests(IntegrationTestFactory factory)
    {
        _factory = factory;
        _client = factory.HttpClient;
    }

    public async Task InitializeAsync()
    {
        // Clean sub_categories first (FK dependency), then categories
        var allSubs = await _client.GetFromJsonAsync<List<SubCategoryDto>>(BaseUrl);
        if (allSubs is not null)
            foreach (var s in allSubs)
                await _client.DeleteAsync($"{BaseUrl}/{s.Id}");

        var allCats = await _client.GetFromJsonAsync<List<CategoryDto>>(CategoriesUrl);
        if (allCats is not null)
            foreach (var c in allCats)
                await _client.DeleteAsync($"{CategoriesUrl}/{c.Id}");

        // Seed a parent category for sub-category tests
        var response = await _client.PostAsJsonAsync(CategoriesUrl, new CreateCategoryRequest("PARENT", "Parent Category"));
        var cat = await response.Content.ReadFromJsonAsync<CategoryDto>();
        _categoryId = cat!.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ───── GET /api/subcategories ─────

    [Fact]
    public async Task GetAll_EmptyDatabase_ReturnsOkWithEmptyList()
    {
        var response = await _client.GetAsync(BaseUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<SubCategoryDto>>();
        Assert.NotNull(list);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetAll_WithData_ReturnsAll()
    {
        await _client.PostAsJsonAsync(BaseUrl, new CreateSubCategoryRequest("S1", "Sub 1", _categoryId));
        await _client.PostAsJsonAsync(BaseUrl, new CreateSubCategoryRequest("S2", "Sub 2", _categoryId));

        var response = await _client.GetAsync(BaseUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<SubCategoryDto>>();
        Assert.Equal(2, list!.Count);
    }

    // ───── GET /api/subcategories/by-category/{categoryId} ─────

    [Fact]
    public async Task GetByCategoryId_ReturnsMatchingSubCategories()
    {
        await _client.PostAsJsonAsync(BaseUrl, new CreateSubCategoryRequest("BC1", "ByCat 1", _categoryId));

        var response = await _client.GetAsync($"{BaseUrl}/by-category/{_categoryId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<SubCategoryDto>>();
        Assert.NotNull(list);
        Assert.Single(list);
        Assert.Equal(_categoryId, list[0].CategoryId);
    }

    [Fact]
    public async Task GetByCategoryId_NoMatch_ReturnsEmptyList()
    {
        var response = await _client.GetAsync($"{BaseUrl}/by-category/99999");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<SubCategoryDto>>();
        Assert.Empty(list!);
    }

    // ───── GET /api/subcategories/{id} ─────

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var createResponse = await _client.PostAsJsonAsync(BaseUrl, new CreateSubCategoryRequest("GBI", "GetById", _categoryId));
        var created = await createResponse.Content.ReadFromJsonAsync<SubCategoryDto>();

        var response = await _client.GetAsync($"{BaseUrl}/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sub = await response.Content.ReadFromJsonAsync<SubCategoryDto>();
        Assert.Equal("GBI", sub!.Code);
        Assert.Equal("GetById", sub.Name);
        Assert.Equal(_categoryId, sub.CategoryId);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"{BaseUrl}/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ───── POST /api/subcategories ─────

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreated()
    {
        var request = new CreateSubCategoryRequest("CR1", "Created Sub", _categoryId);

        var response = await _client.PostAsJsonAsync(BaseUrl, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<SubCategoryDto>();
        Assert.NotNull(created);
        Assert.True(created!.Id > 0);
        Assert.Equal("CR1", created.Code);
        Assert.Equal("Created Sub", created.Name);
        Assert.Equal(_categoryId, created.CategoryId);
        Assert.NotNull(response.Headers.Location);
    }

    // ───── PUT /api/subcategories/{id} ─────

    [Fact]
    public async Task Update_ExistingId_ReturnsOkWithUpdatedData()
    {
        var createResponse = await _client.PostAsJsonAsync(BaseUrl, new CreateSubCategoryRequest("UP1", "Original", _categoryId));
        var created = await createResponse.Content.ReadFromJsonAsync<SubCategoryDto>();

        var updateRequest = new UpdateSubCategoryRequest("UP2", "Updated Sub", null);
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created!.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<SubCategoryDto>();
        Assert.Equal("UP2", updated!.Code);
        Assert.Equal("Updated Sub", updated.Name);
        Assert.Equal(_categoryId, updated.CategoryId);
    }

    [Fact]
    public async Task Update_PartialUpdate_OnlyUpdatesProvidedFields()
    {
        var createResponse = await _client.PostAsJsonAsync(BaseUrl, new CreateSubCategoryRequest("PU1", "Keep Name", _categoryId));
        var created = await createResponse.Content.ReadFromJsonAsync<SubCategoryDto>();

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created!.Id}", new UpdateSubCategoryRequest("PU2", null, null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<SubCategoryDto>();
        Assert.Equal("PU2", updated!.Code);
        Assert.Equal("Keep Name", updated.Name);
    }

    [Fact]
    public async Task Update_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/99999", new UpdateSubCategoryRequest("X", "Y", null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ───── DELETE /api/subcategories/{id} ─────

    [Fact]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        var createResponse = await _client.PostAsJsonAsync(BaseUrl, new CreateSubCategoryRequest("DL1", "Delete Me", _categoryId));
        var created = await createResponse.Content.ReadFromJsonAsync<SubCategoryDto>();

        var response = await _client.DeleteAsync($"{BaseUrl}/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

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
        var createResponse = await _client.PostAsJsonAsync(BaseUrl, new CreateSubCategoryRequest("LF1", "Lifecycle", _categoryId));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<SubCategoryDto>();

        // Read
        var getResponse = await _client.GetAsync($"{BaseUrl}/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // Read by category
        var byCatResponse = await _client.GetAsync($"{BaseUrl}/by-category/{_categoryId}");
        Assert.Equal(HttpStatusCode.OK, byCatResponse.StatusCode);
        var byCatList = await byCatResponse.Content.ReadFromJsonAsync<List<SubCategoryDto>>();
        Assert.Contains(byCatList!, s => s.Id == created.Id);

        // Update
        var updateResponse = await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", new UpdateSubCategoryRequest("LF2", "Updated", null));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<SubCategoryDto>();
        Assert.Equal("LF2", updated!.Code);

        // Delete
        var deleteResponse = await _client.DeleteAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify deleted
        var verifyResponse = await _client.GetAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, verifyResponse.StatusCode);
    }
}
