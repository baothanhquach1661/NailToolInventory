namespace NailToolInventory.ViewModels;

public class UserListViewModel
{
    public List<UserListItemViewModel> Users { get; set; } =
        new();
}


public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsLockedOut { get; set; }

    public bool IsCurrentUser { get; set; }
}