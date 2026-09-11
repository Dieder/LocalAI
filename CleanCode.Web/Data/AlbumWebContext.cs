using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class AlbumWebContext(DbContextOptions<AlbumWebContext> options) : IdentityDbContext<CleanCode.Web.Data.ApplicationUser>(options)
{
}
