namespace TheSoundOfClick;

public class OrderService(OrderRepository orderRepository, UserService userService)
{
    public Task<List<OrderHistoryEntry>> GetCustomerOrderHistoryAsync(Guid customerId)
    {
        return orderRepository.GetOrdersByCustomerAsync(customerId);
    }

    public async Task<List<OrderHistoryEntry>> GetCustomerOrderHistoryForStaffAsync(
        Guid requestingUserId,
        Guid customerId)
    {
        User requestingUser = await userService.GetByID(requestingUserId);
        if (requestingUser.Role is not (UserRole.Employee or UserRole.Admin))
        {
            throw new UnauthorizedAccessException("Only employees and administrators can view customer orders.");
        }

        User customer = await userService.GetByID(customerId);
        if (customer.Role != UserRole.Customer)
        {
            throw new InvalidOperationException("The selected user is not a customer.");
        }

        return await orderRepository.GetOrdersByCustomerAsync(customerId);
    }
}
