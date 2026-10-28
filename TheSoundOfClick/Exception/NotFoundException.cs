namespace TheSoundOfClick;

public class NotFoundException(string message) : Exception(message)
{
  public override string ToString()
  {
    return nameof(NotFoundException) + " " + base.ToString();
  }
}
