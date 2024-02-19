Imports Microsoft.AspNetCore.Components
Imports Microsoft.AspNetCore.Components.Authorization
Imports Microsoft.AspNetCore.Components.Server
Imports Microsoft.AspNetCore.Components.Web
Imports Microsoft.AspNetCore.Identity
Imports Microsoft.Extensions.Options
Imports System.Security.Claims
Imports BlazorAppVBMSAccess.Client
Imports Microsoft.Extensions.DependencyInjection

' This is a server-side AuthenticationStateProvider that revalidates the security stamp for the connected user
' every 30 minutes an interactive circuit is connected. It also uses PersistentComponentState to flow the
' authentication state to the client which is then fixed for the lifetime of the WebAssembly application.
Public NotInheritable Class PersistingRevalidatingAuthenticationStateProvider
    Inherits RevalidatingServerAuthenticationStateProvider
    Private ReadOnly scopeFactory As Microsoft.Extensions.DependencyInjection.IServiceScopeFactory
    Private ReadOnly state As PersistentComponentState
    Private ReadOnly options As IdentityOptions

    Private ReadOnly subscription As PersistingComponentStateSubscription

    Private authenticationStateTask As Task(Of AuthenticationState)

    Public Sub New(ByVal loggerFactory As Microsoft.Extensions.Logging.ILoggerFactory, ByVal serviceScopeFactory As Microsoft.Extensions.DependencyInjection.IServiceScopeFactory, ByVal persistentComponentState As PersistentComponentState, ByVal optionsAccessor As IOptions(Of IdentityOptions))
        MyBase.New(loggerFactory)
        scopeFactory = serviceScopeFactory
        state = persistentComponentState
        options = optionsAccessor.Value

        AddHandler AuthenticationStateChanged, AddressOf OnAuthenticationStateChanged
        subscription = state.RegisterOnPersisting(New Func(Of Task)(AddressOf OnPersistingAsync), RenderMode.InteractiveWebAssembly)
    End Sub

    Protected Overrides ReadOnly Property RevalidationInterval As TimeSpan
        Get
            Return TimeSpan.FromMinutes(30)
        End Get
    End Property

    Protected Overrides Async Function ValidateAuthenticationStateAsync(ByVal authenticationState As AuthenticationState, ByVal cancellationToken As Threading.CancellationToken) As Task(Of Boolean)
        ' Get the user manager from a new scope to ensure it fetches fresh data
        Dim scope = scopeFactory.CreateAsyncScope()
        Dim userManager = scope.ServiceProvider.GetRequiredService(Of UserManager(Of ApplicationUser))()
        Return Await Me.ValidateSecurityStampAsync(userManager, authenticationState.User)
    End Function

    Private Async Function ValidateSecurityStampAsync(ByVal userManager As UserManager(Of ApplicationUser), ByVal principal As ClaimsPrincipal) As Task(Of Boolean)
        Dim user = Await userManager.GetUserAsync(principal)
        If user Is Nothing Then
            Return False
        ElseIf Not userManager.SupportsUserSecurityStamp Then
            Return True
        Else
            Dim principalStamp = principal.FindFirstValue(options.ClaimsIdentity.SecurityStampClaimType)
            Dim userStamp = Await userManager.GetSecurityStampAsync(user)
            Return Equals(principalStamp, userStamp)
        End If
    End Function

    Private Sub OnAuthenticationStateChanged(ByVal task As Task(Of AuthenticationState))
        authenticationStateTask = task
    End Sub

    Private Async Function OnPersistingAsync() As Task
        If authenticationStateTask Is Nothing Then
            Throw New UnreachableException($"Authentication state not set in {NameOf(OnPersistingAsync)}().")
        End If

        Dim authenticationState = Await authenticationStateTask
        Dim principal = authenticationState.User

        If principal.Identity?.IsAuthenticated = True Then
            Dim userId = principal.FindFirst(options.ClaimsIdentity.UserIdClaimType)?.Value
            Dim email = principal.FindFirst(options.ClaimsIdentity.EmailClaimType)?.Value

            If Not Equals(userId, Nothing) AndAlso Not Equals(email, Nothing) Then
                state.PersistAsJson(NameOf(UserInfo), New UserInfo With
                                    {.UserId = userId, .Email = email})
            End If
        End If
    End Function

    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        subscription.Dispose()
        RemoveHandler AuthenticationStateChanged, AddressOf OnAuthenticationStateChanged
        MyBase.Dispose(disposing)
    End Sub
End Class
