using System.ComponentModel.DataAnnotations;
using PhotoIt.Data;

namespace CleanCode.Web.Components.Pages;

public sealed class AlbumFormModel : Album
{
    [Required(ErrorMessage = "Name is required.")]
    public new string Name
    {
        get => base.Name;
        set => base.Name = value;
    }

    [Required(ErrorMessage = "Description is required.")]
    public new string FullDescription
    {
        get => base.FullDescription;
        set => base.FullDescription = value;
    }
}