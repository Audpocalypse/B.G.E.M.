namespace Material_Editor.Services
{
    internal interface IIndexedTokenContext
    {
        string Index { get; }
        string IndexNN { get; }
        string IndexTok { get; }

        string GetLayerIndexText(int zeroBasedLayerIndex);
        string GetLayerIndexNNText(int zeroBasedLayerIndex);
        string GetLayerIndexToken(int zeroBasedLayerIndex);
    }
}
