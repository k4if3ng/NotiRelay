using System.Threading.Tasks;

namespace NotiRelay.Views;

internal interface IUnsavedChangesGuard
{
    bool HasUnsavedChanges { get; }
    Task<bool> ConfirmLeaveAsync();
}
