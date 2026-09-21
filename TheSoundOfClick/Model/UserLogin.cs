namespace TheSoundOfClick;

public class UserLogin
{
  public Int128 UserID { get; init; }
  public byte[] Password { get; set; }

  public UserLogin(Int128 userID, byte[] password)
  {
    UserID = userID;
    Password = password;
  }
}
