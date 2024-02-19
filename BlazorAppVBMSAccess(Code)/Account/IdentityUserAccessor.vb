Imports Microsoft.AspNetCore.Identity

Public NotInheritable Class IdentityUserAccessor
    Property UserManager As UserManager(Of ApplicationUser)
    Property RedirectManager As IdentityRedirectManager
    Public Sub New(pUserManager As UserManager(Of ApplicationUser), pRedirectManager As IdentityRedirectManager)
        UserManager = pUserManager
        RedirectManager = pRedirectManager
    End Sub
    Public Async Function GetRequiredUserAsync(ByVal context As Microsoft.AspNetCore.Http.HttpContext) As Task(Of ApplicationUser)
        Dim user = Await UserManager.GetUserAsync(context.User)
        If user Is Nothing Then
            RedirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{UserManager.GetUserId(context.User)}'.", context)
        End If

        Return user
    End Function
End Class
