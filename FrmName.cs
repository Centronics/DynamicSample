using System.Windows.Forms;

namespace DynamicSample
{
    public partial class FrmName : Form
    {
        public FrmName()
        {
            InitializeComponent();
        }

        public string MyTxtName => txtName.Text;
    }
}
