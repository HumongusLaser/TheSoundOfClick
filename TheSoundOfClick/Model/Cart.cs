namespace TheSoundOfClick;

public class Cart
{
    public Guid ID { get; set; }
    public Guid? UserID { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CartItem
{
    public Guid ID { get; set; }
    public Guid CartID { get; set; }
    public Guid ProductID { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CartLine
{
    public required CartItem Item { get; init; }
    public required Product Product { get; init; }
}
