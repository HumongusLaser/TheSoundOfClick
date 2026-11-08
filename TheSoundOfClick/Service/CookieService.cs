using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace TheSoundOfClick;

public class CookieService(IJSRuntime m_jsRuntime)
{
    public async Task Create(string email, UserRole userRole)
    {
        await m_jsRuntime.InvokeVoidAsync(
            "submitAuthRequest",
            email,
            userRole.ToString());
    }
}
