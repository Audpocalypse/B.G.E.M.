using Material_Editor.AdvancedVariant;
using System.ComponentModel;
using System.Globalization;

namespace Material_Editor.Dialogs
{
    internal sealed class AdvancedRuleRow : INotifyPropertyChanged
    {
        private string layer1MatchText = string.Empty;
        private string layer2MatchText = string.Empty;
        private string layer3MatchText = string.Empty;
        private string layer4MatchText = string.Empty;
        private string indexTok = string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Layer1MatchText
        {
            get => layer1MatchText;
            set => SetField(ref layer1MatchText, value ?? string.Empty, nameof(Layer1MatchText));
        }

        public string Layer2MatchText
        {
            get => layer2MatchText;
            set => SetField(ref layer2MatchText, value ?? string.Empty, nameof(Layer2MatchText));
        }

        public string Layer3MatchText
        {
            get => layer3MatchText;
            set => SetField(ref layer3MatchText, value ?? string.Empty, nameof(Layer3MatchText));
        }

        public string Layer4MatchText
        {
            get => layer4MatchText;
            set => SetField(ref layer4MatchText, value ?? string.Empty, nameof(Layer4MatchText));
        }

        public string IndexTok
        {
            get => indexTok;
            set => SetField(ref indexTok, value ?? string.Empty, nameof(IndexTok));
        }

        public bool HasMeaningfulContent =>
            !string.IsNullOrWhiteSpace(Layer1MatchText)
            || !string.IsNullOrWhiteSpace(Layer2MatchText)
            || !string.IsNullOrWhiteSpace(Layer3MatchText)
            || !string.IsNullOrWhiteSpace(Layer4MatchText)
            || !string.IsNullOrWhiteSpace(IndexTok);

        public AdvancedVariantRule ToRule(int sourceOrder)
        {
            return new AdvancedVariantRule
            {
                Layer1Index = ParseLayerMatch(Layer1MatchText),
                Layer2Index = ParseLayerMatch(Layer2MatchText),
                Layer3Index = ParseLayerMatch(Layer3MatchText),
                Layer4Index = ParseLayerMatch(Layer4MatchText),
                IndexToken = NormalizeToken(IndexTok),
                SourceOrder = sourceOrder
            };
        }

        private static int? ParseLayerMatch(string value)
        {
            return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : null;
        }

        private static string NormalizeToken(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private void SetField(ref string field, string value, string propertyName)
        {
            if (field == value)
                return;

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
