namespace TheSoundOfClick;

public interface IPaymentProcessor
{
  public void ProcessPayment(decimal amount);
}
