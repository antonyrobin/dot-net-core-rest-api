namespace dot_net_core_rest_api.Entities;

public class SubCategory
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
}
