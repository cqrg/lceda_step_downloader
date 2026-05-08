using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using lceda_step_downloader.Models.Root;

namespace lceda_step_downloader.Views
{
    public partial class RootView : HandyControl.Controls.Window
    {
        public RootView()
        {
            InitializeComponent();
        }

        private void ListViewItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is ListViewItem item && item.DataContext is ResultItem resultItem)
            {
                if (!string.IsNullOrEmpty(resultItem.PriceInfo))
                {
                    item.ToolTip = resultItem.PriceInfo;
                }
                else
                {
                    item.ToolTip = "价格加载中...";
                }
            }
        }
    }
}
