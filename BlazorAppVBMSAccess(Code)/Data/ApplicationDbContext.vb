Imports Microsoft.AspNetCore.Identity.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore

Public Class ApplicationDbContext
    Inherits IdentityDbContext(Of ApplicationUser)

    Public Sub New(options As DbContextOptions(Of ApplicationDbContext))
        MyBase.New(options)
    End Sub

End Class
