namespace TheSoundOfClick;

public class ProductService
{
    private readonly ProductRepository _productRepository;

    public ProductService(ProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<List<Product>> GetAllProductsAsync()
    {
        return await _productRepository.GetProductsAsync();
    }
}