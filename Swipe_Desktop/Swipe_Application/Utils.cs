using System.Windows.Media;
using System.Windows;

namespace Swipe_Application
{
class Utils
{
    static public T? FindParent<T>(DependencyObject child)
        where T : DependencyObject
    {
        DependencyObject parent = VisualTreeHelper.GetParent(child);

        while (parent != null)
        {
            if (parent is T)
            {
                return (T)parent;
            }
            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
    }
}
}
