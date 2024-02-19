Imports Microsoft.AspNetCore.Identity
Imports Microsoft.AspNetCore.Identity.UI.Services

' Remove the "else if (EmailSender is IdentityNoOpEmailSender)" block from RegisterConfirmation.razor after updating with a real implementation.
Public NotInheritable Class IdentityNoOpEmailSender
    Implements IEmailSender(Of ApplicationUser)
    Private ReadOnly emailSender As IEmailSender = New NoOpEmailSender()

    Public Function SendConfirmationLinkAsync(ByVal user As ApplicationUser, ByVal email As String, ByVal confirmationLink As String) As Task Implements IEmailSender(Of ApplicationUser).SendConfirmationLinkAsync
        Return emailSender.SendEmailAsync(email, "Confirm your email", $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.")
    End Function

    Public Function SendPasswordResetLinkAsync(ByVal user As ApplicationUser, ByVal email As String, ByVal resetLink As String) As Task Implements IEmailSender(Of ApplicationUser).SendPasswordResetLinkAsync
        Return emailSender.SendEmailAsync(email, "Reset your password", $"Please reset your password by <a href='{resetLink}'>clicking here</a>.")
    End Function

    Public Function SendPasswordResetCodeAsync(ByVal user As ApplicationUser, ByVal email As String, ByVal resetCode As String) As Task Implements IEmailSender(Of ApplicationUser).SendPasswordResetCodeAsync
        Return emailSender.SendEmailAsync(email, "Reset your password", $"Please reset your password using the following code: {resetCode}")
    End Function
End Class
