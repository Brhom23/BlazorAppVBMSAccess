Imports Microsoft.AspNetCore.Authentication
Imports Microsoft.AspNetCore.Components.Authorization
Imports Microsoft.AspNetCore.Http.Extensions
Imports Microsoft.AspNetCore.Identity
Imports System.Security.Claims
Imports System.Text.Json
Imports System.Runtime.CompilerServices
Imports Microsoft.AspNetCore.Routing
Imports Microsoft.AspNetCore.Builder
Imports Microsoft.AspNetCore.Http
Imports Microsoft.Extensions.Primitives
Imports Microsoft.AspNetCore.Http.HttpResults
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions
Imports Microsoft.Extensions.Logging

Public Module IdentityComponentsEndpointRouteBuilderExtensions

    ' These endpoints are required by the Identity Razor components defined in the /Components/Account/Pages directory of this project.
    <Extension()>
    Public Function MapAdditionalIdentityEndpoints(ByVal endpoints As IEndpointRouteBuilder) As IEndpointConventionBuilder
        ArgumentNullException.ThrowIfNull(endpoints)

        Dim accountGroup = endpoints.MapGroup("/Account")

        accountGroup.MapPost("/PerformExternalLogin",
                             Function(ByVal context As HttpContext, ByVal signInManager As SignInManager(Of ApplicationUser), ByVal provider As String, ByVal returnUrl As String) As ChallengeHttpResult
                                 Dim query As IEnumerable(Of KeyValuePair(Of String, StringValues)) =
                                                          New List(Of KeyValuePair(Of String, StringValues)) From
                                                          {New KeyValuePair(Of String, StringValues)("ReturnUrl", returnUrl),
                                                           New KeyValuePair(Of String, StringValues)("Action", LOGINCALLBACKACTION)}
                                 Dim redirectUrl = UriHelper.BuildRelative(
                                                          context.Request.PathBase,
                                                          "/Account/ExternalLogin",
                                                          QueryString.Create(query))
                                 Dim properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl)
                                 Return TypedResults.Challenge(properties, {provider})
                             End Function)

        accountGroup.MapPost("/Logout",
                             Async Function(ByVal user As ClaimsPrincipal, ByVal signInManager As SignInManager(Of ApplicationUser), ByVal returnUrl As String)
                                 Await signInManager.SignOutAsync()
                                 Return TypedResults.LocalRedirect($"~/{returnUrl}")
                             End Function)

        Dim manageGroup = accountGroup.MapGroup("/Manage").RequireAuthorization()

        manageGroup.MapPost("/LinkExternalLogin",
                            Async Function(ByVal context As HttpContext, ByVal signInManager As SignInManager(Of ApplicationUser), ByVal provider As String)
                                ' Clear the existing external cookie to ensure a clean login process
                                Await context.SignOutAsync(IdentityConstants.ExternalScheme)
                                Dim redirectUrl = UriHelper.BuildRelative(context.Request.PathBase, "/Account/Manage/ExternalLogins", QueryString.Create("Action", LINKLOGINCALLBACKACTION))
                                Dim properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, signInManager.UserManager.GetUserId(context.User))
                                Return
                            End Function)

        Dim loggerFactory = endpoints.ServiceProvider.GetRequiredService(Of Logging.ILoggerFactory)()
        Dim downloadLogger = loggerFactory.CreateLogger("DownloadPersonalData")

        manageGroup.MapPost("/DownloadPersonalData",
                            Async Function(ByVal context As HttpContext, ByVal userManager As UserManager(Of ApplicationUser), ByVal authenticationStateProvider As AuthenticationStateProvider)
                                Dim user = Await userManager.GetUserAsync(context.User)
                                If user Is Nothing Then
                                    Return Results.NotFound($"Unable to load user with ID '{userManager.GetUserId(context.User)}'.")
                                End If

                                Dim userId = Await userManager.GetUserIdAsync(user)
                                downloadLogger.LogInformation("User with ID '{UserId}' asked for their personal data.", userId)

                                ' Only include personal data for download
                                Dim personalData = New Dictionary(Of String, String)()
                                Dim personalDataProps = GetType(ApplicationUser).GetProperties().Where(Function(prop) Attribute.IsDefined(prop, GetType(PersonalDataAttribute)))
                                For Each p In personalDataProps
                                    personalData.Add(p.Name, If(p.GetValue(user)?.ToString(), "null"))
                                Next

                                Dim logins = Await userManager.GetLoginsAsync(user)
                                For Each l In logins
                                    personalData.Add($"{l.LoginProvider} external login provider key", l.ProviderKey)
                                Next

                                ''' Cannot convert InvocationExpressionSyntax, System.InvalidCastException: Unable to cast object of type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.EmptyStatementSyntax' to type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentListSyntax'.
                                '''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitInvocationExpression(InvocationExpressionSyntax node)
                                '''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
                                '''    at ICSharpCode.CodeConverter.VB.CommentConvertingVisitorWrapper`1.Accept(SyntaxNode csNode, Boolean addSourceMapping)
                                ''' 
                                ''' Input:
                                ''' 
                                '''                 personalData.Add("Authenticator Key", (await userManager.GetAuthenticatorKeyAsync(user))!)
                                ''' 
                                Dim fileBytes = JsonSerializer.SerializeToUtf8Bytes(personalData)

                                context.Response.Headers.TryAdd("Content-Disposition", "attachment; filename=PersonalData.json")
                                Return TypedResults.File(fileBytes, contentType:="application/json", fileDownloadName:="PersonalData.json")
                            End Function)

        Return accountGroup
    End Function
End Module
