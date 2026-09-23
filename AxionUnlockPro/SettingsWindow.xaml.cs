using System.Windows;
using Axion.Core.Config;

namespace AxionUnlockPro
{
    public partial class SettingsWindow : Window
    {
        private readonly AppSettings _s;

        public SettingsWindow(AppSettings settings)
        {
            InitializeComponent();
            _s = settings;
            txtAdb.Text = _s.AdbPath;
            txtFb.Text = _s.FastbootPath;
            txtEdl.Text = _s.EdlPath;
            txtMtk.Text = _s.MtkPath;
            txtProg.Text = _s.ProgrammersRoot;
            chkAuto.IsChecked = _s.AutoSelectBrand;
            chkLog.IsChecked = _s.PersistLog;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _s.AdbPath = txtAdb.Text.Trim();
            _s.FastbootPath = txtFb.Text.Trim();
            _s.EdlPath = txtEdl.Text.Trim();
            _s.MtkPath = txtMtk.Text.Trim();
            _s.ProgrammersRoot = txtProg.Text.Trim();
            _s.AutoSelectBrand = chkAuto.IsChecked == true;
            _s.PersistLog = chkLog.IsChecked == true;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
