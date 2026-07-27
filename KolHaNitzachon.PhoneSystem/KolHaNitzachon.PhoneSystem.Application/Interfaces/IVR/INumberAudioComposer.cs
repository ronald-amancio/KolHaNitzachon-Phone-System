namespace KolHaNitzachon.PhoneSystem.Application.Interfaces.IVR
{
    public interface INumberAudioComposer
    {
        IReadOnlyList<string> Compose(int number);
    }
}