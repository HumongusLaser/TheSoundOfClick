using Microsoft.AspNetCore.Identity;

namespace TheSoundOfClick;

public class UserLogin
{
  public Int128 UserID { get; init; }
    public byte[] PasswordHash { get; set; }

    public UserLogin(Int128 userID, byte[] passwordHash)
  {
    UserID = userID;
    PasswordHash = passwordHash;
  }
}
