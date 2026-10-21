namespace TheSoundOfClick
{
    public class ProductService
    {
        private readonly List<Product> products = new List<Product>
        {
            new Keyboard(
                1, 10, "Gaming Keyboard", 599.95m, "Black",
                "Nordic", true, "Red Switches",
                "Mekanisk gaming tastatur", "TKL", 800, 1000
            ),

            new Keycap(
                2, 25, "Pink Keycaps", 199.95m, "Pink",
                "Pastel", true,
                "Et sæt lyserøde keycaps til mekaniske tastaturer",
                104, 150
            ),

            new Mousepad(
                3, 15, "Gaming Mousepad", 149.95m, "Black",
                900, 400, 4,
                "Stor musemåtte til gaming", 600, 750
            )
        };

        public List<Product> GetAllProducts()
        {
            return products;
        }

        public Product? GetProductById(Int128 id)
        {
            return products.FirstOrDefault(product => product.ID == id);
        }
    }
}