namespace TheSoundOfClick;

public class DiscountService
{
  private DiscountRepository m_disRepo;
  private List<IDiscount> m_discounts = new List<IDiscount>();

  public DiscountService(DiscountRepository discountRepository)
  {
    m_disRepo = discountRepository;
  }
}
