namespace TheSoundOfClick;

public class ProductService
{
    private readonly ProductRepository _productRepository;

    public ProductService(ProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<List<Product>> GetAllProductsAsync(Guid? id, string? category, int batchSize)
    {
        return await _productRepository.GetProductsAsync(id, category, batchSize);
    }

    public async Task<(List<Product> Products, int TotalCount)> GetProductPageAsync(
        int offset,
        int pageSize,
        string? category,
        string? search,
        string sortOrder,
        CancellationToken cancellationToken = default)
    {
        return await _productRepository.GetProductPageAsync(
            offset,
            pageSize,
            category,
            search,
            sortOrder,
            cancellationToken);
    }
    
    public async Task<Product> GetProductAsync(Guid id)
    {
        return await _productRepository.GetProductAsync(id);
    }

    public async Task CreateProductAsync(Product product)
    {
        product.ID = await _productRepository.CreateAsync(product);
    }
}