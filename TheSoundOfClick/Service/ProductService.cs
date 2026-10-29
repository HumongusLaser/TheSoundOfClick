namespace TheSoundOfClick;

public class ProductService
{
    private readonly ProductRepository _productRepository;

    public ProductService(ProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<List<Product>> GetAllProductsAsync(Guid? id, string category, int batchSize)
    {
        return await _productRepository.GetProductsAsync(id, category, batchSize);
    }
    
    public async Task<Product> GetProductAsync(Guid id)
    {
        return await _productRepository.GetProductAsync(id);
    }
}