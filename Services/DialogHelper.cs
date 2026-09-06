using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace BarodaPOS.Services
{
    public static class DialogHelper
    {
        public static async Task ShowMessageAsync(string title, string message)
        {
            if (Application.Current == null) return;
            var tcs = new TaskCompletionSource<bool>();
            _ = Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    var textBlock = new System.Windows.Controls.TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 14,
                        UseLayoutRounding = true,
                        SnapsToDevicePixels = true,
                        Margin = new Thickness(0,0,0,12)
                    };
                    TextOptions.SetTextFormattingMode(textBlock, TextFormattingMode.Display);
                    TextOptions.SetTextRenderingMode(textBlock, TextRenderingMode.ClearType);

                    var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = title,
                        Content = textBlock,
                        CloseButtonText = "OK",
                        UseLayoutRounding = true,
                        SnapsToDevicePixels = true
                    };
                    TextOptions.SetTextFormattingMode(uiMessageBox, TextFormattingMode.Display);
                    TextOptions.SetTextRenderingMode(uiMessageBox, TextRenderingMode.ClearType);

                    await uiMessageBox.ShowDialogAsync();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            await tcs.Task;
        }

        public static async Task<bool> ShowConfirmAsync(string title, string message)
        {
            if (Application.Current == null) return false;
            var tcs = new TaskCompletionSource<bool>();
            _ = Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    var textBlock = new System.Windows.Controls.TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 14,
                        UseLayoutRounding = true,
                        SnapsToDevicePixels = true,
                        Margin = new Thickness(0,0,0,12)
                    };
                    TextOptions.SetTextFormattingMode(textBlock, TextFormattingMode.Display);
                    TextOptions.SetTextRenderingMode(textBlock, TextRenderingMode.ClearType);

                    var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = title,
                        Content = textBlock,
                        PrimaryButtonText = "Yes",
                        CloseButtonText = "No",
                        UseLayoutRounding = true,
                        SnapsToDevicePixels = true
                    };
                    TextOptions.SetTextFormattingMode(uiMessageBox, TextFormattingMode.Display);
                    TextOptions.SetTextRenderingMode(uiMessageBox, TextRenderingMode.ClearType);

                    var result = await uiMessageBox.ShowDialogAsync();
                    tcs.SetResult(result == Wpf.Ui.Controls.MessageBoxResult.Primary);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return await tcs.Task;
        }
    }
}
