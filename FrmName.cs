using System.Windows.Forms;

namespace DynamicSample
{
    public partial class FrmName : Form
    {
        public FrmName()
        {
            InitializeComponent();
        }

        public string MyTxtName
        {
            get => txtName.Text;
            set => txtName.Text = value;
        }

        void txtName_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch ((Keys)e.KeyChar)
            {
                case Keys.Enter:
                case Keys.Tab:
                case Keys.Escape:
                case Keys.Pause:
                case Keys.XButton1:
                case Keys.RButton | Keys.Enter:
                case Keys.RButton | Keys.FinalMode:
                    e.Handled = true;
                    return;
            }
        }

        void txtName_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    DialogResult = DialogResult.OK;
                    return;
                case Keys.Escape:
                    DialogResult = DialogResult.Cancel;
                    return;
            }
        }

        void FrmName_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    DialogResult = DialogResult.Cancel;
                    return;
            }
        }
    }
}
