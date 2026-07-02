namespace OwariNakiTobira
{
    public interface IRuntimeResettable
    {
        int ResetOrder { get; }
        void RuntimeReset();
    }
}
