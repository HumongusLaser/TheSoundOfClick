using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace TheSoundOfClick;

public class CartService(
    CartRepository cartRepository,
    OrderRepository orderRepository,
    UserService userService,
    ProtectedLocalStorage localStorage)
{
    private const string AnonymousCartStorageKey = "TheSoundOfClick.CartId";

    public Order? LastCheckoutOrder { get; private set; }

    public async Task<CartItem> AddToCartAsync(Guid productId, Guid? userId = null, int quantity = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        if (userId is not Guid customerId)
        {
            throw new UnauthorizedAccessException("Du skal være logget ind som kunde for at lægge varer i kurven.");
        }

        User user = await userService.GetByID(customerId);
        if (user.Role != UserRole.Customer)
        {
            throw new UnauthorizedAccessException("Kun kunder kan lægge varer i kurven.");
        }

        Cart cart = await GetOrCreateCurrentCartAsync(userId);

        return await cartRepository.AddCartItemAsync(cart.ID, productId, quantity);
    }

    public async Task<List<CartLine>> GetCurrentCartLinesAsync(Guid? userId = null)
    {
        Cart? cart = await GetCurrentCartAsync(userId);
        return cart is null ? [] : await cartRepository.GetCartLinesAsync(cart.ID);
    }

    private async Task<Cart> GetOrCreateCurrentCartAsync(Guid? userId)
    {
        Cart? cart = await GetCurrentCartAsync(userId);
        if (cart is not null)
        {
            return cart;
        }

        cart = await cartRepository.CreateCartAsync(userId);
        if (!userId.HasValue)
        {
            await localStorage.SetAsync(AnonymousCartStorageKey, cart.ID);
        }

        return cart;
    }

    private async Task<Cart?> GetCurrentCartAsync(Guid? userId)
    {
        if (userId.HasValue)
        {
            return await cartRepository.GetCartByUserIdAsync(userId.Value);
        }

        var storedCartId = await localStorage.GetAsync<Guid>(AnonymousCartStorageKey);
        if (!storedCartId.Success)
        {
            return null;
        }

        Cart? cart = await cartRepository.GetCartByIdAsync(storedCartId.Value);
        if (cart is null)
        {
            await localStorage.DeleteAsync(AnonymousCartStorageKey);
        }

        return cart;
    }

    public async Task<Cart> CreateCartAsync(Guid? userId = null)
    {
        return await cartRepository.CreateCartAsync(userId);
    }

    public async Task UpdateCartAsync(Guid cartId, Guid? userId)
    {
        await cartRepository.UpdateCartAsync(cartId, userId);
    }

    public async Task DeleteCartAsync(Guid cartId)
    {
        await cartRepository.DeleteCartAsync(cartId);
    }

    public async Task<CartItem> CreateCartItemAsync(Guid cartId, Guid productId, int quantity)
    {
        return await cartRepository.CreateCartItemAsync(cartId, productId, quantity);
    }

    public async Task UpdateCartItemAsync(Guid cartItemId, int quantity)
    {
        await cartRepository.UpdateCartItemAsync(cartItemId, quantity);
    }

    public async Task DeleteCartItemAsync(Guid cartItemId)
    {
        await cartRepository.DeleteCartItemAsync(cartItemId);
    }

    public async Task<Order> CheckoutAsync(Guid? userId)
    {
        LastCheckoutOrder = null;
        if (userId is not Guid customerId)
        {
            throw new UnauthorizedAccessException("Du skal være logget ind for at gennemføre et køb.");
        }

        User customer = await userService.GetByID(customerId);
        if (customer.Role != UserRole.Customer)
        {
            throw new UnauthorizedAccessException("Kun kunder kan gennemføre et køb.");
        }

        Cart? cart = await GetCurrentCartAsync(customerId);
        if (cart is null)
        {
            throw new InvalidOperationException("Din kurv er tom.");
        }

        string shippingAddress = string.Join(
            ", ",
            new[] { customer.StreetAddress, customer.Apartment, $"{customer.PostalCode} {customer.City}" }
                .Where(part => !string.IsNullOrWhiteSpace(part)));
        Order order = await orderRepository.CheckoutCartAsync(cart.ID, customerId, shippingAddress);
        LastCheckoutOrder = order;
        return order;
    }
}
