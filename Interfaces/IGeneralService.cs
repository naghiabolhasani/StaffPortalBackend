using StaffPortal.Models;

namespace StaffPortal.Interfaces;

public interface IGeneralService
{
    Task<List<GeneralTemplateMessageResponse>> GetTemplateMessagesAsync(
        int kind,
        string? message = null);
}