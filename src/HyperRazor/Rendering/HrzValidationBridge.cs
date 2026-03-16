using HyperRazor.Components;
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
    private HrzFormView? _formView;
    private HrzValidationRootId? _resolvedRootId;
    private bool _registrationRefreshPending;

    [Inject]
    private IHrzFieldPathResolver FieldPathResolver { get; set; } = default!;

    [Inject]
    private IHrzForms HrzForms { get; set; } = default!;

    [Parameter]
    public string? FormName { get; set; }

    [Parameter]
    public HrzValidationRootId? RootId { get; set; }

    [Parameter]
    public HrzLiveValidationOptions? Live { get; set; }

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
            ClearFormView(_currentEditContext);
            _currentEditContext = CurrentEditContext;
            _messageStore = new ValidationMessageStore(CurrentEditContext);
        }

        _resolvedRootId = RootId ?? ResolveRootId(FormName);
        _formView = HrzForms.For(
            _currentEditContext.Model,
            _resolvedRootId,
            live: Live);
        _formView.RegistrationChanged += RequestRegistrationRefresh;
        _currentEditContext.Properties[typeof(HrzFormView)] = _formView;

        ApplySubmitValidationState();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_formView?.Live is null)
        {
            return;
        }

        builder.OpenComponent<HrzLivePolicyRegion>(0);
        builder.AddAttribute(1, nameof(HrzLivePolicyRegion.Form), _formView);
        builder.CloseComponent();
    }

    public void Dispose()
    {
        ClearMessages(notify: true);
        ClearFormView(_currentEditContext);
    }

    private void ApplySubmitValidationState()
    {
        if (_currentEditContext is null || _messageStore is null || _resolvedRootId is null)
        {
            return;
        }

        _messageStore.Clear();

        var validationState = HttpContext?.GetSubmitValidationState(_resolvedRootId);
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

    private static HrzValidationRootId ResolveRootId(string? formName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(formName);
        return new HrzValidationRootId(formName);
    }

    private void RequestRegistrationRefresh()
    {
        if (_registrationRefreshPending)
        {
            return;
        }

        _registrationRefreshPending = true;
        _ = InvokeAsync(() =>
        {
            _registrationRefreshPending = false;
            StateHasChanged();
        });
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

    private static void ClearFormView(EditContext? editContext)
    {
        _ = editContext?.Properties.Remove(typeof(HrzFormView));
    }
}
