using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ollez.Converters
{
    public static class DocumentBindingBehavior
    {
        public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.RegisterAttached(
                "Document",
                typeof(FlowDocument),
                typeof(DocumentBindingBehavior),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnDocumentChanged)));

        public static void SetDocument(DependencyObject obj, FlowDocument value)
        {
            obj.SetValue(DocumentProperty, value);
        }

        public static FlowDocument GetDocument(DependencyObject obj)
        {
            return (FlowDocument)obj.GetValue(DocumentProperty);
        }

        private static void OnDocumentChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is RichTextBox rtb)
            {
                rtb.Document = e.NewValue as FlowDocument ?? new FlowDocument();
            }
        }
    }
}