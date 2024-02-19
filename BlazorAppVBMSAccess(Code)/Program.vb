Imports BlazorAppVBMSAccess_Code_
Imports BlazorAppVBMSAccess.Client.Pages
Imports Microsoft.AspNetCore.Identity
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.AspNetCore.Builder
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports Microsoft.AspNetCore.Components.Authorization

Public Class Program(Of TApp)
    Public Shared Sub Main(args As String())
        Dim builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args)

        ' Add services to the container.
        Microsoft.Extensions.DependencyInjection.ServerRazorComponentsBuilderExtensions.AddInteractiveServerComponents(Microsoft.Extensions.DependencyInjection.RazorComponentsServiceCollectionExtensions.AddRazorComponents(builder.Services)).AddInteractiveWebAssemblyComponents()

        builder.Services.AddCascadingAuthenticationState()
        builder.Services.AddScoped(Of IdentityUserAccessor)()
        builder.Services.AddScoped(Of IdentityRedirectManager)()
        builder.Services.AddScoped(Of AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider)()

        AuthenticationServiceCollectionExtensions.AddAuthentication(builder.Services,
                                                                    Sub(options)
                                                                        options.DefaultScheme = IdentityConstants.ApplicationScheme
                                                                        options.DefaultSignInScheme = IdentityConstants.ExternalScheme
                                                                    End Sub).AddIdentityCookies()

        Dim connectionString As String = builder.Configuration.GetConnectionString("DefaultConnection")
        If connectionString Is Nothing Then
            Throw New InvalidOperationException("Connection string 'DefaultConnection' not found.")
        End If

        builder.Services.AddDbContext(Of ApplicationDbContext)(
            Sub(options) options.UseSqlServer(
                connectionString,
                Sub(b) b.MigrationsAssembly("BlazorAppVBMSAccess")'لا بد منها لإتمام عملية الترحيل
                    ))

        builder.Services.AddDatabaseDeveloperPageExceptionFilter()

        IdentityBuilderExtensions.AddSignInManager(IdentityEntityFrameworkBuilderExtensions.AddEntityFrameworkStores(Of ApplicationDbContext)(builder.Services.AddIdentityCore(Of ApplicationUser)(CType(Sub(options) options.SignIn.RequireConfirmedAccount = True, Action(Of IdentityOptions))))).AddDefaultTokenProviders()

        builder.Services.AddSingleton(Of IEmailSender(Of ApplicationUser), IdentityNoOpEmailSender)()

        Dim app = builder.Build()

        ' Configure the HTTP request pipeline.
        If app.Environment.IsDevelopment() Then
            app.UseWebAssemblyDebugging()
            app.UseMigrationsEndPoint()
        Else
            app.UseExceptionHandler("/Error")
            ' The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts()
        End If

        app.UseHttpsRedirection()

        app.UseStaticFiles()
        app.UseAntiforgery()

        Microsoft.AspNetCore.Builder.WebAssemblyRazorComponentsEndpointConventionBuilderExtensions.AddInteractiveWebAssemblyRenderMode(Microsoft.AspNetCore.Builder.ServerRazorComponentsEndpointConventionBuilderExtensions.AddInteractiveServerRenderMode(Microsoft.AspNetCore.Builder.RazorComponentsEndpointRouteBuilderExtensions.MapRazorComponents(Of TApp)(app))).AddAdditionalAssemblies(GetType(Counter).Assembly)

        ' Add additional endpoints required by the Identity /Account Razor components.
        app.MapAdditionalIdentityEndpoints()

        app.Run()
    End Sub


End Class
