using Npgsql;

namespace TheSoundOfClick
{
    public class ProductService
    {
        private NpgsqlDataSource _dataSource;
        public ProductService(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }
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

        public (List<Product> Items, int Total) GetProducts( Func<NpgsqlCommand, IEnumerable<Filter>> buildFilters,
            string sortOrder, int page, int pageSize)
        {
            pageSize = Math.Clamp(pageSize, 1, 20);
            page = Math.Max(1, page);

            // Whitelisted, never built from user input directly
            var orderBy = sortOrder switch
            {
                "priceAsc"  => "price ASC, id ASC",
                "priceDesc" => "price DESC, id ASC",
                _           => "id ASC"
            };

            using var conn = _dataSource.OpenConnection(); // however you get your connection

            using var countCmd = new NpgsqlCommand { Connection = conn };
            FilterService.AddFilters(countCmd, "SELECT COUNT(*) FROM products", buildFilters(countCmd));
            var total = Convert.ToInt32(countCmd.ExecuteScalar());

            using var cmd = new NpgsqlCommand { Connection = conn };
            FilterService.AddFilters(cmd, "SELECT * FROM products", buildFilters(cmd));
            cmd.CommandText += $" ORDER BY {orderBy} LIMIT @limit OFFSET @offset";
            cmd.Parameters.AddWithValue("limit", pageSize);
            cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);

            var items = new List<Product>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                items.Add(ProductMapping(reader)); // your existing mapping to Keyboard/Keycap/Mousepad

            return (items, total);
        }

        public static Product ProductMapping(NpgsqlDataReader reader)
        {
            var category    = reader.GetString(reader.GetOrdinal("category"));
            var detailsJson = reader.GetString(reader.GetOrdinal("details"));   // your JSON column

            return ProductFactory.Create(category, detailsJson);
        }
    }
}