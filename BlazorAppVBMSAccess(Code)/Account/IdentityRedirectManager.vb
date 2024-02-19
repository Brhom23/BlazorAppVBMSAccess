Imports Microsoft.AspNetCore.Components
Imports System.Diagnostics.CodeAnalysis

Public NotInheritable Class IdentityRedirectManager
    Public Const StatusCookieName As String = "Identity.StatusMessage"

    Private Shared ReadOnly StatusCookieBuilder As New Microsoft.AspNetCore.Http.CookieBuilder With
    {.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
        .HttpOnly = True,
        .IsEssential = True,
        .MaxAge = System.TimeSpan.FromSeconds(5)}

    Property NavigationManager As NavigationManager
    Public Sub New(pNavigationManager As NavigationManager)
        NavigationManager = pNavigationManager
    End Sub

    <DoesNotReturn>
    Public Sub RedirectTo(ByVal uri As String)

        uri = If(uri, "")

        ' Prevent open redirects.
        If Not System.Uri.IsWellFormedUriString(uri, UriKind.Relative) Then
            uri = NavigationManager.ToBaseRelativePath(uri)
        End If

        ' During static rendering, NavigateTo throws a NavigationException which is handled by the framework as a redirect.
        ' So as long as this is called from a statically rendered Identity component, the InvalidOperationException is never thrown.
        NavigationManager.NavigateTo(uri)
        Throw New InvalidOperationException($"{NameOf(IdentityRedirectManager)} can only be used during static rendering.")
    End Sub

    <DoesNotReturn>
    Public Sub RedirectTo(ByVal uri As String, ByVal queryParameters As Dictionary(Of String, Object))
        Dim uriWithoutQuery = NavigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path)
        Dim newUri = NavigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters)
        Me.RedirectTo(newUri)
    End Sub

    <DoesNotReturn>
    Public Sub RedirectToWithStatus(ByVal uri As String, ByVal message As String, ByVal context As Microsoft.AspNetCore.Http.HttpContext)
        context.Response.Cookies.Append(StatusCookieName, message, IdentityRedirectManager.StatusCookieBuilder.Build(context))
        Me.RedirectTo(uri)
    End Sub

    Private ReadOnly Property CurrentPath As String
        Get
            Return NavigationManager.ToAbsoluteUri(CStr(NavigationManager.Uri)).GetLeftPart(UriPartial.Path)
        End Get
    End Property

    <DoesNotReturn>
    Public Sub RedirectToCurrentPage()
        Me.RedirectTo(CurrentPath)
    End Sub

    <DoesNotReturn>
    Public Sub RedirectToCurrentPageWithStatus(ByVal message As String, ByVal context As Microsoft.AspNetCore.Http.HttpContext)
        RedirectToWithStatus(CurrentPath, message, context)
    End Sub
End Class