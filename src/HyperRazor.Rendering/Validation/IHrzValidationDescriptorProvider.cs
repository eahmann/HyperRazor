namespace HyperRazor.Rendering;

public interface IHrzValidationDescriptorProvider
{
    HrzValidationDescriptor GetDescriptor(Type modelType);
}
