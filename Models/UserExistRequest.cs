namespace StaffPortal.Models;

public class UserExistRequest
{
    public string Mobile { get; set; } = string.Empty;
}

public class UserExistResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public UserExistData? Data { get; set; }
}

public class UserExistData
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Username { get; set; }
}