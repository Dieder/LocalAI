using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PhotoIt.Web.Data;

public class AlbumWebContext(DbContextOptions<AlbumWebContext> options) : IdentityDbContext<ApplicationUser>(options)
{
}
