using System.Windows.Input;
using System.Windows;

namespace AsfMojoUI
{
    public static class DropBehavior
    {
        public static ICommand GetPreviewDropCommand(UIElement obj)
        {
            return (ICommand)obj.GetValue(PreviewDropCommandProperty);
        }
        public static void SetPreviewDropCommand(UIElement obj, ICommand command)
        {
            obj.SetValue(PreviewDropCommandProperty, command);
        }
        public static readonly DependencyProperty PreviewDropCommandProperty =
                    DependencyProperty.RegisterAttached
                    (
                        "PreviewDropCommand",
                        typeof(ICommand),
                        typeof(DropBehavior),
                        new PropertyMetadata(OnCommandChanged)
                    );
        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement obj = d as UIElement;
            ICommand command = GetPreviewDropCommand(obj);
            obj.Drop += (sender, e1) =>
            {
                string[] array = e1.Data.GetData(DataFormats.FileDrop) as string[];
                if (array != null)
                {
                    command.Execute(array);
                }
                e1.Handled = true;
            };
        }

    }

}
