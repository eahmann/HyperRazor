using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;
using HyperRazor.Components.Validation;

namespace HyperRazor.Rendering;

public sealed class HrzValidationBridge : ComponentBase, IDisposable
{
    private ValidationMessageStore? _messageStore;
    private EditContext? _currentEditContext;

    [Inject]
    private IHrzFieldPathResolver FieldPathResolver { get; set; } = default!;

    [Parameter, EditorRequired]
    public HrzValidationRootId RootId { get; set; } = default!;

    [CascadingParameter]
    private EditContext? CurrentEditContext { get; set; }

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    protected override void OnParametersSet()
    {
        if (CurrentEditContext is null)
        {
            throw new InvalidOperationException($"{nameof(HrzValidationBridge)} requires a cascading {nameof(EditContext)}.");
        }

        if (!ReferenceEquals(_currentEditContext, CurrentEditContext))
        {
            ClearMessages(notify: false);
            _currentEditContext = CurrentEditContext;
            _messageStore = new ValidationMessageStore(CurrentEditContext);
        }

        ApplySubmitValidationState();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
    }

    public void Dispose()
    {
        ClearMessages(notify: true);
    }

    private void ApplySubmitValidationState()
    {
        if (_currentEditContext is null || _messageStore is null)
        {
            return;
        }

        _messageStore.Clear();

        var validationState = HttpContext?.GetSubmitValidationState(RootId);
        if (validationState is not null)
        {
            foreach (var summaryError in validationState.SummaryErrors)
            {
                _messageStore.Add(new FieldIdentifier(_currentEditContext.Model, string.Empty), summaryError);
            }

            foreach (var fieldError in validationState.FieldErrors)
            {
                var fieldIdentifier = FieldPathResolver.Resolve(_currentEditContext.Model, fieldError.Key);
                _messageStore.Add(fieldIdentifier, fieldError.Value);
            }
        }

        _currentEditContext.NotifyValidationStateChanged();
    }

    private void ClearMessages(bool notify)
    {
        if (_messageStore is null || _currentEditContext is null)
        {
            return;
        }

        _messageStore.Clear();
        if (notify)
        {
            _currentEditContext.NotifyValidationStateChanged();
        }
    }
}
