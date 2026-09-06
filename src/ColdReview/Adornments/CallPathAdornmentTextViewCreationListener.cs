using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace ColdReview.Adornments
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class CallPathAdornmentTextViewCreationListener : IWpfTextViewCreationListener
    {
        public const string LayerName = "ColdReviewCallPath";

        [Export(typeof(AdornmentLayerDefinition))]
        [Name(LayerName)]
        [Order(After = PredefinedAdornmentLayers.Caret)]
        [TextViewRole(PredefinedTextViewRoles.Document)]
        internal AdornmentLayerDefinition EditorAdornmentLayer;

        public void TextViewCreated(IWpfTextView textView)
        {
            try
            {
                textView.Properties.GetOrCreateSingletonProperty(() => new CallPathHud(textView));
            }
            catch (Exception)
            {
            }
        }
    }
}
